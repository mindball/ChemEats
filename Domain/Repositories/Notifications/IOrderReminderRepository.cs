using Domain.Entities;
using Domain.Models.Notifications;

namespace Domain.Repositories.Notifications;

public interface IOrderReminderRepository
{
    // Opted-in users without a Pending/Completed order for the date of an active menu whose
    // ordering deadline falls within (now, now + leadTime], and who were not reminded for that menu yet.
    Task<List<OrderReminderCandidate>> GetDueRemindersAsync(DateTime now, TimeSpan leadTime, CancellationToken cancellationToken = default);
    Task AddReminderLogsAsync(IEnumerable<MenuReminderLog> logs, CancellationToken cancellationToken = default);
}
