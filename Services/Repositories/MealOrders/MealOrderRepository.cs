using Domain;
using Domain.Entities;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Services.Repositories.MealOrders;

public class MealOrderRepository : IMealOrderRepository
{
    private readonly AppDbContext _dbContext;

    public MealOrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    #region Base Query

    private IQueryable<OrderJoinRow> BuildBaseQuery(
        string userId,
        bool includeDeleted = false,
        bool onlyDeleted = false)
    {
        IQueryable<MealOrder> moQuery = _dbContext.MealOrders.AsNoTracking();

        if (includeDeleted)
            moQuery = moQuery.IgnoreQueryFilters();

        if (onlyDeleted)
            moQuery = moQuery.Where(mo => mo.IsDeleted);

        return
            from mo in moQuery
            join meal in _dbContext.Meals.AsNoTracking()
                on mo.MealId equals meal.Id
            join menu in _dbContext.Menus.AsNoTracking()
                on meal.MenuId equals menu.Id
            join supplier in _dbContext.Suppliers.AsNoTracking()
                on menu.SupplierId equals supplier.Id
            where mo.UserId == userId
            select new OrderJoinRow
            {
                OrderId = mo.Id,
                UserId = mo.UserId,
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
                PortionAmount = mo.PortionAmount
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

        if (startDate.HasValue)
            query = query.Where(x => x.OrderedAt >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.OrderedAt < endDate.Value.Date.AddDays(1));

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

        if (order.Status is MealOrderStatus.Completed or MealOrderStatus.Cancelled)
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
                g.Key.SupplierName ?? string.Empty,
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
                g.Key.SupplierName ?? string.Empty,
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
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName ?? string.Empty,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status.ToString(),
                x.PortionApplied,
                x.IsDeleted,
                x.PortionAmount,
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m))
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
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName ?? string.Empty,
                x.OrderedAt,
                x.MenuDate,
                x.Price,
                x.Status.ToString(),
                x.PortionApplied,
                x.IsDeleted,
                x.PortionAmount,
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m))
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
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m)) // NetAmount
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPaidAsync(
        Guid orderId,
        string userId,
        DateTime paidOn,
        CancellationToken cancellationToken = default)
    {
        MealOrder? order = await _dbContext.MealOrders
            .FirstOrDefaultAsync(
                x => x.Id == orderId && x.UserId == userId,
                cancellationToken);

        if (order is null)
            throw new InvalidOperationException("Order not found.");

        order.MarkAsPaid(paidOn);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

        public async Task<(int PaidCount, decimal TotalPaid)> MarkOrderAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> orderIds,
        DateTime paidOn,
        CancellationToken cancellationToken = default)
    {
        List<MealOrder> orders = await _dbContext.MealOrders
            .Where(x => x.UserId == userId && orderIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        decimal totalPaid = 0m;
        int paidCount = 0;

        foreach (MealOrder order in orders)
        {
            if (order.PaymentStatus == PaymentStatus.Paid)
                continue;

            if (order.Status == MealOrderStatus.Cancelled)
                continue;

            order.MarkAsPaid(paidOn);
            totalPaid += order.GetNetAmount();
            paidCount++;
        }

        if (paidCount > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return (paidCount, totalPaid);
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
        DateTime start = date.ToDateTime(TimeOnly.MinValue);
        DateTime endExclusive = date.ToDateTime(TimeOnly.MinValue).AddDays(1);

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

        if (startDate.HasValue)
            query = query.Where(x => x.OrderedAt >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.OrderedAt < endDate.Value.Date.AddDays(1));

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        return await query
            .OrderBy(x => x.OrderedAt)
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
                Math.Max(0m, x.Price - (x.PortionApplied ? x.PortionAmount : 0m))
            ))
            .ToListAsync(cancellationToken);
    }

    #endregion

    // Local projection type
    private sealed class OrderJoinRow
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
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
    }
}