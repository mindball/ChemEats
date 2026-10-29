namespace WebApi.Infrastructure.Notifications;

public interface IEmailService
{
    // Throws on delivery failure so callers can decide whether to retry.
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
