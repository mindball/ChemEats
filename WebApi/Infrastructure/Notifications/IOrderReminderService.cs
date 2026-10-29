namespace WebApi.Infrastructure.Notifications;

public interface IOrderReminderService
{
    // Returns the number of reminder emails sent.
    Task<int> SendDueRemindersAsync(DateTime now, CancellationToken cancellationToken = default);
}
