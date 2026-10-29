using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.Notifications;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            throw new InvalidOperationException("EmailSettings:SmtpHost not configured");

        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
            throw new InvalidOperationException("EmailSettings:FromAddress not configured");

        // SmtpClient.EnableSsl performs STARTTLS (ports 587/25); implicit TLS on port 465 is not supported.
        using SmtpClient client = new(_settings.SmtpHost, _settings.SmtpPort > 0 ? _settings.SmtpPort : 587)
        {
            EnableSsl = _settings.EnableSsl || _settings.EnableStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        if (!string.IsNullOrWhiteSpace(_settings.UserName))
            client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);

        using MailMessage message = new()
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        _logger.LogInformation("Sending email to {To} via {Host}:{Port} ({Subject})",
            toEmail, _settings.SmtpHost, _settings.SmtpPort, subject);

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("Email sent to {To} ({Subject})", toEmail, subject);
    }
}
