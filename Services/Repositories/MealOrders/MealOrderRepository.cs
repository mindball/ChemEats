using Domain;
using Domain.Entities;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.MealOrders;

public class MealOrderRepository : IMealOrderRepository
{
    private readonly AppDbContext _dbContext;

    public MealOrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    #region Base Query

    // A null userId returns rows for all users (admin-wide queries).
    private IQueryable<OrderJoinRow> BuildBaseQuery(
        string? userId,
        bool includeDeleted = false,
        bool onlyDeleted = false)
    {
        IQueryable<MealOrder> moQuery = _dbContext.MealOrders.AsNoTracking();

        if (includeDeleted)
            moQuery = moQuery.IgnoreQueryFilters();

        if (onlyDeleted)
            moQuery = moQuery.Where(mo => mo.IsDeleted);

        if (userId is not null)
            moQuery = moQuery.Where(mo => mo.UserId == userId);

        return
            from mo in moQuery
            join meal in _dbContext.Meals.AsNoTracking()
                on mo.MealId equals meal.Id
            join menu in _dbContext.Menus.AsNoTracking()
                on meal.MenuId equals menu.Id
            join supplier in _dbContext.Suppliers.AsNoTracking()
                on menu.SupplierId equals supplier.Id
            join user in _dbContext.Users.AsNoTracking()
                on mo.UserId equals user.Id
            join paidBy in _dbContext.Users.AsNoTracking()
                on mo.PaidByUserId equals paidBy.Id into paidByUsers
            from paidBy in paidByUsers.DefaultIfEmpty()
            select new OrderJoinRow
            {
                OrderId = mo.Id,
                UserId = mo.UserId,
                EmployeeName = user.FullName,
                MealId = meal.Id,
                MealName = meal.Name,
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                MenuDate = mo.MenuDate,
                OrderedAt = mo.OrderedAt,
                Price = mo.PriceAmount,
                Status = mo.Status,
                IsDeleted = mo.IsDeleted,
                PaymentStatus = mo.PaymentStatus,
                PaidOn = mo.PaidOn,
                PortionApplied = mo.PortionApplied,
                PortionAmount = mo.PortionAmount,
                PaidByName = paidBy == null ? null : paidBy.FullName
            };
    }

    private static IQueryable<OrderJoinRow> ApplyFilters(
        IQueryable<OrderJoinRow> query,
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        MealOrderStatus? status = null)
    {
        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        // Filter by the day the meal is for, the same as the payment pages.
        if (startDate.HasValue)
            query = query.Where(x => x.MenuDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.MenuDate < endDate.Value.Date.AddDays(1));

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return query;
    }

    #endregion

    #region CRUD

    public async Task<MealOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealOrders
            .AsNoTracking()
            .Include(mo => mo.Meal)
            .Include(mo => mo.User)
            .FirstOrDefaultAsync(mo => mo.Id == id, cancellationToken);
    }

    public async Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mealOrder);

        bool mealExists = await _dbContext.Meals
            .AsNoTracking()
            .AnyAsync(m => m.Id == mealOrder.MealId, cancellationToken);

        if (!mealExists)
            throw new ArgumentException($"Meal with id '{mealOrder.MealId}' does not exist.");

        if (mealOrder.User != null)
            _dbContext.Attach(mealOrder.User);

        await _dbContext.MealOrders.AddAsync(mealOrder, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        MealOrder? order = await _dbContext.MealOrders
            .FirstOrDefaultAsync(mo => mo.Id == orderId, cancellationToken);

        if (order is null)
            return;

        order.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Queries – Summary

    public async Task<IReadOnlyList<UserOrderSummary>> GetUserOrdersAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId);
        query = ApplyFilters(query, supplierId, startDate, endDate);

        return await query
            .GroupBy(x => new
            {
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status,
                x.IsDeleted
            })
            .Select(g => new UserOrderSummary(
                g.Key.MealId,
                g.Key.MealName,
                g.Key.SupplierId,
                g.Key.SupplierName,
                g.Key.OrderedAt,
                g.Key.MenuDate,
                g.Count(),
                g.Key.Price,
                g.Select(x => x.OrderId).ToList(),
                g.Key.Status.ToString(),
                g.Key.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderSummary>> GetUserOrdersAsync(
        string userId,
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        bool includeDeleted,
        bool onlyDeleted,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId, includeDeleted, onlyDeleted);
        query = ApplyFilters(query, supplierId, startDate, endDate);

        return await query
            .GroupBy(x => new
            {
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status,
                x.IsDeleted
            })
            .Select(g => new UserOrderSummary(
                g.Key.MealId,
                g.Key.MealName,
                g.Key.SupplierId,
                g.Key.SupplierName,
                g.Key.OrderedAt,
                g.Key.MenuDate,
                g.Count(),
                g.Key.Price,
                g.Select(x => x.OrderId).ToList(),
                g.Key.Status.ToString(),
                g.Key.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Queries – Items

    public async Task<IReadOnlyList<UserOrderItem>> GetOrdersByMenuAsync(
        string userId,
        Guid menuId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from mo in _dbContext.MealOrders.AsNoTracking()
                    .Where(mo => mo.UserId == userId)

                join user in _dbContext.Users.AsNoTracking()
                    on mo.UserId equals user.Id

                join meal in _dbContext.Meals.AsNoTracking()
                    on mo.MealId equals meal.Id

                join menu in _dbContext.Menus.AsNoTracking()
                    on meal.MenuId equals menu.Id

                join supplier in _dbContext.Suppliers.AsNoTracking()
                    on menu.SupplierId equals supplier.Id

                where menu.Id == menuId
                orderby mo.OrderedAt

                select new UserOrderItem(
                    mo.Id,
                    mo.UserId,
                    user.FullName,  
                    meal.Id,
                    meal.Name,
                    supplier.Id,
                    supplier.Name,
                    mo.OrderedAt,
                    mo.MenuDate,
                    mo.PriceAmount,
                    mo.Status.ToString(),
                    mo.PortionApplied,
                    mo.IsDeleted,
                    mo.PortionAmount,
                    Math.Max(0m, mo.PriceAmount -
                                 (mo.PortionApplied ? mo.PortionAmount : 0m)),
                    mo.PaymentStatus == PaymentStatus.Paid,
                    mo.PaidOn
                ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderItem>> GetAllOrdersByMenuAsync(
        Guid menuId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from mo in _dbContext.MealOrders.AsNoTracking()
                join meal in _dbContext.Meals.AsNoTracking()
                    on mo.MealId equals meal.Id
                join menu in _dbContext.Menus.AsNoTracking()
                    on meal.MenuId equals menu.Id
                join supplier in _dbContext.Suppliers.AsNoTracking()
                    on menu.SupplierId equals supplier.Id
                join user in _dbContext.Users.AsNoTracking()
                    on mo.UserId equals user.Id
                where menu.Id == menuId
                orderby user.FullName, mo.OrderedAt
                select new UserOrderItem(
                    mo.Id,
                    mo.UserId,
                    user.FullName,
                    meal.Id,
                    meal.Name,
                    supplier.Id,
                    supplier.Name,
                    mo.OrderedAt,
                    mo.MenuDate,
                    mo.PriceAmount,
                    mo.Status.ToString(),
                    mo.PortionApplied,
                    mo.IsDeleted,
                    mo.PortionAmount,
                    Math.Max(0m, mo.PriceAmount - (mo.PortionApplied ? mo.PortionAmount : 0m)),
                    mo.PaymentStatus == PaymentStatus.Paid,
                    mo.PaidOn
                ))
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeDeleted = false,
        MealOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId, includeDeleted);
        query = ApplyFilters(query, supplierId, startDate, endDate, status);

        return await query
            .OrderBy(x => x.OrderedAt)
            .Select(x => new UserOrderItem(
                x.OrderId,
                x.UserId,
                x.EmployeeName,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status.ToString(),
                x.PortionApplied,
                x.IsDeleted,
                x.PortionAmount,
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)),
                x.PaymentStatus == PaymentStatus.Paid,
                x.PaidOn
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        bool includeDeleted,
        bool onlyDeleted,
        MealOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId, includeDeleted, onlyDeleted);
        query = ApplyFilters(query, supplierId, startDate, endDate, status);

        return await query
            .OrderBy(x => x.OrderedAt)
            .Select(x => new UserOrderItem(
                x.OrderId,
                x.UserId,
                x.EmployeeName,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status.ToString(),
                x.PortionApplied,
                x.IsDeleted,
                x.PortionAmount,
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)),
                x.PaymentStatus == PaymentStatus.Paid,
                x.PaidOn
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserOutstandingSummary> GetUserOutstandingSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId)
            .Where(x => x.PaymentStatus == PaymentStatus.Unpaid && x.Status == MealOrderStatus.Completed);

        decimal totalOutstanding = await query
            .Select(x => Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)))
            .SumAsync(cancellationToken);

        int unpaidCount = await query
            .CountAsync(cancellationToken);

        return new UserOutstandingSummary(
            userId,
            totalOutstanding,
            unpaidCount);
    }

    public async Task<IReadOnlyList<UserOrderPaymentItem>> GetUnpaidOrdersAsync(
        string userId,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId);

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        return await query
            .Where(x => x.PaymentStatus == PaymentStatus.Unpaid)
            .OrderBy(x => x.OrderedAt)
            .Select(x => new UserOrderPaymentItem(
                x.OrderId,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,                  // snapshot price
                x.PaymentStatus,
                x.PaidOn,
                x.PortionApplied,         // New
                x.PortionAmount,          // New
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)), // NetAmount
                x.Status == MealOrderStatus.Cancelled,
                x.Status == MealOrderStatus.Completed,
                x.PaidByName
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPaidAsync(
        Guid orderId,
        string userId,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default)
    {
        MealOrder? order = await _dbContext.MealOrders
            .FirstOrDefaultAsync(
                x => x.Id == orderId && x.UserId == userId,
                cancellationToken);

        if (order is null)
            throw new InvalidOperationException("Order not found.");

        order.MarkAsPaid(paidOn, paidByUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(int PaidCount, decimal TotalPaid)> MarkOrderAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> orderIds,
        DateTime paidOn,
        decimal companyPortion,
        string paidByUserId,
        CancellationToken cancellationToken = default)
    {
        // Only completed orders (finalized menus) are payable; pending and cancelled ones are skipped
        // instead of failing the whole batch.
        List<MealOrder> orders = await _dbContext.MealOrders
            .Where(x => x.UserId == userId
                        && orderIds.Contains(x.Id)
                        && x.PaymentStatus == PaymentStatus.Unpaid
                        && x.Status == MealOrderStatus.Completed)
            .ToListAsync(cancellationToken);

        return await PayOrdersAsync(userId, orders, paidOn, companyPortion, paidByUserId, cancellationToken);
    }

    public async Task<(int PaidCount, decimal TotalPaid)> MarkAllUnpaidAsPaidAsync(
        string userId,
        DateTime? upToDate,
        DateTime paidOn,
        decimal companyPortion,
        string paidByUserId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MealOrder> query = _dbContext.MealOrders
            .Where(x => x.UserId == userId
                        && x.PaymentStatus == PaymentStatus.Unpaid
                        && x.Status == MealOrderStatus.Completed);

        // No date means every unpaid completed order, regardless of its menu date.
        if (upToDate.HasValue)
        {
            DateTime upToExclusive = upToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.MenuDate < upToExclusive);
        }

        List<MealOrder> orders = await query.ToListAsync(cancellationToken);

        return await PayOrdersAsync(userId, orders, paidOn, companyPortion, paidByUserId, cancellationToken);
    }

    private async Task<(int PaidCount, decimal TotalPaid)> PayOrdersAsync(
        string userId,
        List<MealOrder> orders,
        DateTime paidOn,
        decimal companyPortion,
        string paidByUserId,
        CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
            return (0, 0m);

        if (companyPortion > 0m)
        {
            HashSet<DateOnly> datesWithPortionApplied = (await _dbContext.MealOrders
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.PortionApplied && !x.IsDeleted)
                .Select(x => x.MenuDate)
                .Distinct()
                .ToListAsync(cancellationToken))
                .Select(d => DateOnly.FromDateTime(d))
                .ToHashSet();

            foreach (IGrouping<DateOnly, MealOrder> group in orders
                .GroupBy(o => DateOnly.FromDateTime(o.MenuDate)))
            {
                if (datesWithPortionApplied.Contains(group.Key))
                    continue;

                // The portion goes to the most expensive order of the day, the same rule the outstanding summary uses.
                MealOrder mostExpensive = group
                    .OrderByDescending(o => o.PriceAmount)
                    .ThenBy(o => o.OrderedAt)
                    .First();

                mostExpensive.ApplyPortion(Math.Min(companyPortion, mostExpensive.PriceAmount));
                datesWithPortionApplied.Add(group.Key);
            }
        }

        decimal totalPaid = 0m;
        int paidCount = 0;

        foreach (MealOrder order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            order.MarkAsPaid(paidOn, paidByUserId);
            totalPaid += order.GetNetAmount();
            paidCount++;
        }

        if (paidCount > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return (paidCount, totalPaid);
    }

    public async Task<(int CompletedCount, decimal TotalAmount)> MarkPendingOrdersAsCompletedForMenuAsync(
        Guid menuId,
        CancellationToken cancellationToken = default)
    {
        List<MealOrder> pendingOrders = await _dbContext.MealOrders
            .Include(mo => mo.Meal)
            .Where(mo => mo.Meal.MenuId == menuId
                         && mo.Status == MealOrderStatus.Pending
                         && !mo.IsDeleted)
            .ToListAsync(cancellationToken);

        int completedCount = 0;
        decimal totalAmount = 0m;

        foreach (MealOrder order in pendingOrders)
        {
            order.MarkAsCompleted();
            totalAmount += order.GetNetAmount();
            completedCount++;
        }

        if (completedCount > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return (completedCount, totalAmount);
    }

    public async Task<IReadOnlyList<Guid>> GetExpiredMenuIdsWithPendingOrdersAsync(
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        // Deleted orders and deleted menus are excluded by their global query filters.
        return await (
                from mo in _dbContext.MealOrders.AsNoTracking()
                join meal in _dbContext.Meals.AsNoTracking()
                    on mo.MealId equals meal.Id
                join menu in _dbContext.Menus.AsNoTracking()
                    on meal.MenuId equals menu.Id
                where mo.Status == MealOrderStatus.Pending && menu.ActiveUntil <= now
                select menu.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetExpectedPortionsAsync(
        string userId,
        IReadOnlyCollection<DateTime> menuDates,
        decimal companyPortion,
        CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, decimal> expected = new();

        if (companyPortion <= 0m || menuDates.Count == 0)
            return expected;

        HashSet<DateTime> days = menuDates.Select(d => d.Date).ToHashSet();
        DateTime from = days.Min();
        DateTime toExclusive = days.Max().AddDays(1);

        // All of the user's orders for those days, regardless of supplier, so one portion per day is shared across suppliers.
        var dayOrders = await _dbContext.MealOrders
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Status != MealOrderStatus.Cancelled
                        && x.MenuDate >= from
                        && x.MenuDate < toExclusive)
            .Select(x => new { x.Id, x.MenuDate, x.OrderedAt, x.PriceAmount, x.PortionApplied, x.PaymentStatus })
            .ToListAsync(cancellationToken);

        foreach (var day in dayOrders
            .Where(x => days.Contains(x.MenuDate.Date))
            .GroupBy(x => x.MenuDate.Date))
        {
            if (day.Any(x => x.PortionApplied))
                continue;

            // Same rule as PayOrdersAsync: the most expensive order of the day gets the portion.
            var mostExpensive = day
                .Where(x => x.PaymentStatus == PaymentStatus.Unpaid)
                .OrderByDescending(x => x.PriceAmount)
                .ThenBy(x => x.OrderedAt)
                .FirstOrDefault();

            if (mostExpensive is not null)
                expected[mostExpensive.Id] = Math.Min(companyPortion, mostExpensive.PriceAmount);
        }

        return expected;
    }

    public async Task<Dictionary<Guid, int>> GetPendingOrdersCountByMenuIdsAsync(
        IEnumerable<Guid> menuIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> menuIdList = menuIds.ToList();

        return await _dbContext.MealOrders
            .AsNoTracking()
            .Include(mo => mo.Meal)
            .Where(mo => menuIdList.Contains(mo.Meal.MenuId)
                         && mo.Status == MealOrderStatus.Pending
                         && !mo.IsDeleted)
            .GroupBy(mo => mo.Meal.MenuId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }

    public Task<bool> HasAppliedPortionAsync(string userId, Guid menuId, DateOnly date, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> HasPortionAppliedOnDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        // DateTime start = date.ToDateTime(TimeOnly.MinValue);
        // DateTime endExclusive = date.ToDateTime(TimeOnly.MinValue).AddDays(1);

        return await _dbContext.MealOrders
            .AsNoTracking()
            .AnyAsync(mo =>
                mo.UserId == userId
                && !mo.IsDeleted
                && mo.PortionApplied,
                cancellationToken);
    }

    #endregion

    #region Queries - Payments

    public async Task<IReadOnlyList<UserOrderPaymentItem>> GetAllOrdersForPeriodAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderJoinRow> query = BuildBaseQuery(userId);

        // Payments are grouped by the day the meal is for, matching the outstanding summary.
        if (startDate.HasValue)
            query = query.Where(x => x.MenuDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.MenuDate < endDate.Value.Date.AddDays(1));

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        var result = await query
            .OrderBy(x => x.MenuDate)
            .ThenBy(x => x.OrderedAt)
            .Select(x => new UserOrderPaymentItem(
                x.OrderId,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.PaymentStatus,
                x.PaidOn,
                x.PortionApplied,
                x.PortionAmount,
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)),
                x.Status == MealOrderStatus.Cancelled,
                x.Status == MealOrderStatus.Completed,
                x.PaidByName
            ))
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<EmployeeOutstandingSummary>> GetOutstandingSummariesAsync(
        DateTime? startDate,
        DateTime? endDate,
        decimal companyPortion,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        // A null start means no "earlier" split; a null end means no upper bound (all unpaid orders).
        DateTime periodStart = startDate?.Date ?? DateTime.MinValue;

        // No lower bound: unpaid orders before the period are reported separately as "earlier" debt,
        // because paying an employee settles everything up to the end date.
        IQueryable<OrderJoinRow> periodQuery = BuildBaseQuery(userId);

        if (endDate.HasValue)
        {
            DateTime periodEndExclusive = endDate.Value.Date.AddDays(1);
            periodQuery = periodQuery.Where(x => x.MenuDate < periodEndExclusive);
        }

        // Aggregated per user and menu date in the database: the company portion is granted once per user per day,
        // so the per-day totals are needed to compute the expected portion the same way PayOrdersAsync does.
        List<DailyOutstandingRow> dailyRows = await periodQuery
            .Where(x => x.PaymentStatus == PaymentStatus.Unpaid && x.Status == MealOrderStatus.Completed)
            .GroupBy(x => new { x.UserId, x.EmployeeName, x.MenuDate })
            .Select(g => new DailyOutstandingRow(
                g.Key.UserId,
                g.Key.EmployeeName,
                g.Key.MenuDate,
                g.Count(),
                g.Sum(x => x.Price),
                g.Sum(x => x.PortionApplied ? x.PortionAmount : 0m),
                g.Max(x => x.Price)))
            .ToListAsync(cancellationToken);

        if (dailyRows.Count == 0)
            return [];

        HashSet<(string UserId, DateOnly Day)> daysWithPortion = (await periodQuery
                .Where(x => x.PortionApplied)
                .Select(x => new { x.UserId, x.MenuDate })
                .Distinct()
                .ToListAsync(cancellationToken))
            .Select(x => (x.UserId, DateOnly.FromDateTime(x.MenuDate)))
            .ToHashSet();

        List<EmployeeOutstandingSummary> summaries = [];

        foreach (IGrouping<string, DailyOutstandingRow> userRows in dailyRows.GroupBy(r => r.UserId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int unpaidCount = 0;
            decimal grossAmount = 0m;
            decimal portionAmount = 0m;
            int earlierCount = 0;
            decimal earlierNet = 0m;

            foreach (IGrouping<DateOnly, DailyOutstandingRow> dayRows in userRows
                .GroupBy(r => DateOnly.FromDateTime(r.MenuDate)))
            {
                int dayCount = dayRows.Sum(r => r.OrderCount);
                decimal dayGross = dayRows.Sum(r => r.GrossAmount);
                decimal dayPortion = dayRows.Sum(r => r.AppliedPortion);

                // A day that already has a portion (e.g. on a paid order) gets no new one at payment time.
                // PayOrdersAsync applies the portion to the most expensive order, capped at its price.
                if (companyPortion > 0m && !daysWithPortion.Contains((userRows.Key, dayRows.Key)))
                    dayPortion += Math.Min(companyPortion, dayRows.Max(r => r.MaxPrice));

                if (dayRows.Key.ToDateTime(TimeOnly.MinValue) < periodStart)
                {
                    earlierCount += dayCount;
                    earlierNet += Math.Max(0m, dayGross - dayPortion);
                    continue;
                }

                unpaidCount += dayCount;
                grossAmount += dayGross;
                portionAmount += dayPortion;
            }

            summaries.Add(new EmployeeOutstandingSummary(
                userRows.Key,
                userRows.First().EmployeeName,
                unpaidCount,
                grossAmount,
                portionAmount,
                Math.Max(0m, grossAmount - portionAmount),
                earlierCount,
                earlierNet));
        }

        return summaries
            .OrderBy(s => s.EmployeeName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.UserId, StringComparer.Ordinal)
            .ToList();
    }

    #endregion

    private sealed record DailyOutstandingRow(
        string UserId,
        string EmployeeName,
        DateTime MenuDate,
        int OrderCount,
        decimal GrossAmount,
        decimal AppliedPortion,
        decimal MaxPrice);

    // Local projection type
    private sealed class OrderJoinRow
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string EmployeeName { get; init; } = string.Empty;
        public Guid MealId { get; init; }
        public string MealName { get; init; } = string.Empty;
        public Guid SupplierId { get; init; }
        public string SupplierName { get; init; } = string.Empty;
        public DateTime OrderedAt { get; init; }
        public DateTime MenuDate { get; init; }
        public decimal Price { get; init; }
        public MealOrderStatus Status { get; init; }
        public bool IsDeleted { get; init; }
        public PaymentStatus PaymentStatus { get; init; }
        public DateTime? PaidOn { get; init; }
        public bool PortionApplied { get; init; }
        public decimal PortionAmount { get; init; }
        public string? PaidByName { get; init; }
    }
}