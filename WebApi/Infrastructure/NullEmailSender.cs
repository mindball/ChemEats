

using System.Net;
using System.Net.Mail;
using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Serilog;

namespace WebApi.Infrastructure;

public class SmtpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => SendAsync(email, subject, htmlMessage);

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => SendAsync(email, "Confirm your email",
            $"<p>Hello ,</p><p>Please confirm your email by <a href='{confirmationLink}'>clicking here</a>.</p>");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => SendAsync(email, "Reset your password",
            $"<p>Hello {user.UserName},</p><p>You can reset your password by <a href='{resetLink}'>clicking here</a>.</p>");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => SendAsync(email, "Your password reset code",
            $"<p>Hello {user.UserName},</p><p>Your reset code is: <strong>{resetCode}</strong></p>");

    private async Task SendAsync(string toEmail, string subject, string htmlMessage)
    {
        try
        {
            toEmail = "mincho.balaliew@gmail.com";
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000 // 15 сек за всеки случай
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            _logger.LogInformation("📧 Sending email to {To} via {Host}:{Port} ({Subject})",
                toEmail, _settings.SmtpHost, _settings.SmtpPort, subject);

            await client.SendMailAsync(mail);

            Log.Information("✅ Email sent successfully to {To}", toEmail);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "❌ SMTP error while sending email to {To}", toEmail);
            // Не хвърляме нагоре – само логваме
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ General error while sending email to {To}", toEmail);
        }
    }
}




public sealed class EmailSettings
{
    public string FromName { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool EnableStartTls { get; set; }
    public bool EnableSsl { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}