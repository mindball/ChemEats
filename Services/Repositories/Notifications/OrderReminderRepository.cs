using Domain;
using Domain.Entities;
using Domain.Models.Notifications;
using Domain.Repositories.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Notifications;

public class OrderReminderRepository : IOrderReminderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderReminderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<OrderReminderCandidate>> GetDueRemindersAsync(
        DateTime now,
        TimeSpan leadTime,
        CancellationToken cancellationToken = default)
    {
        DateTime deadlineLimit = now.Add(leadTime);

        // Soft-deleted menus are excluded by the Menu query filter.
        var dueMenus = await (
                from menu in _dbContext.Menus.AsNoTracking()
                join supplier in _dbContext.Suppliers.AsNoTracking()
                    on menu.SupplierId equals supplier.Id
                where menu.ActiveUntil > now && menu.ActiveUntil <= deadlineLimit
                select new { menu.Id, menu.Date, menu.ActiveUntil, SupplierName = supplier.Name })
            .ToListAsync(cancellationToken);

        if (dueMenus.Count == 0)
            return [];

        var optedInUsers = await _dbContext.Users.AsNoTracking()
            .Where(u => u.OrderRemindersEnabled && u.NotificationEmail != null)
            .Select(u => new { u.Id, u.FullName, NotificationEmail = u.NotificationEmail! })
            .ToListAsync(cancellationToken);

        if (optedInUsers.Count == 0)
            return [];

        List<Guid> menuIds = dueMenus.Select(m => m.Id).ToList();
        List<string> userIds = optedInUsers.Select(u => u.Id).ToList();

        HashSet<(string UserId, Guid MenuId)> alreadyReminded = (await _dbContext.MenuReminderLogs.AsNoTracking()
                .Where(r => menuIds.Contains(r.MenuId))
                .Select(r => new { r.UserId, r.MenuId })
                .ToListAsync(cancellationToken))
            .Select(r => (r.UserId, r.MenuId))
            .ToHashSet();

        // Date ranges are computed client-side; DateTime.Date is not reliably translatable on Firebird.
        DateTime rangeStart = dueMenus.Min(m => m.Date).Date;
        DateTime rangeEnd = dueMenus.Max(m => m.Date).Date.AddDays(1);

        // Deleted orders are excluded by the MealOrder query filter; cancelled orders do not count as ordered.
        HashSet<(string UserId, DateTime Date)> orderedDates = (await _dbContext.MealOrders.AsNoTracking()
                .Where(mo => userIds.Contains(mo.UserId)
                             && mo.Status != MealOrderStatus.Cancelled
                             && mo.MenuDate >= rangeStart
                             && mo.MenuDate < rangeEnd)
                .Select(mo => new { mo.UserId, mo.MenuDate })
                .ToListAsync(cancellationToken))
            .Select(mo => (mo.UserId, mo.MenuDate.Date))
            .ToHashSet();

        List<OrderReminderCandidate> candidates = [];

        foreach (var menu in dueMenus.OrderBy(m => m.Date).ThenBy(m => m.ActiveUntil))
        {
            foreach (var user in optedInUsers)
            {
                if (alreadyReminded.Contains((user.Id, menu.Id)) || orderedDates.Contains((user.Id, menu.Date.Date)))
                    continue;

                candidates.Add(new OrderReminderCandidate(
                    user.Id,
                    user.FullName,
                    user.NotificationEmail,
                    menu.Id,
                    menu.Date,
                    menu.ActiveUntil,
                    menu.SupplierName));
            }
        }

        return candidates;
    }

    public async Task AddReminderLogsAsync(IEnumerable<MenuReminderLog> logs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logs);

        await _dbContext.MenuReminderLogs.AddRangeAsync(logs, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Drop the failed rows so a later save on this context does not retry them.
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
