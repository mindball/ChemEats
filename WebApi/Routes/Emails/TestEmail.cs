using System.Net.Mail;
using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Routes.Emails;

public static class TestEmail
{
    public static void EmailEndpoints(this WebApplication app)
    {
        app.MapGet("/test-email", async (IEmailSender<ApplicationUser> emailSender, ILogger<Program> logger) =>
        {
            string testRecipient = "твоя_имейл@пример.com"; // сложи твоя имейл тук
            string subject = "SMTP Test Email";
            string body = "<p>This is a test email from ChemEats API.</p>";

            try
            {
                logger.LogInformation("🧪 Starting test email to {To}", testRecipient);

                await emailSender.SendConfirmationLinkAsync(null, subject, body);

                logger.LogInformation("✅ Test email sent successfully to {To}", testRecipient);

                return Results.Ok(new { Success = true, Message = $"Email sent to {testRecipient}" });
            }
            catch (SmtpException smtpEx)
            {
                logger.LogError(smtpEx, "❌ SMTP error while sending test email to {To}", testRecipient);
                return Results.BadRequest(new { Success = false, Message = smtpEx.Message, Type = "SmtpException" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ General error while sending test email to {To}", testRecipient);
                return Results.BadRequest(new { Success = false, Message = ex.Message, Type = "GeneralException" });
            }
        });

    }
}