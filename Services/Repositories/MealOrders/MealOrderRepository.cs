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
                Date = mo.Date,
                MenuDate = menu.Date,
                Price = meal.Price.Amount,
                Status = mo.Status,
                IsDeleted = mo.IsDeleted
            };
    }

    private static IQueryable<OrderJoinRow> ApplyFilters(
        IQueryable<OrderJoinRow> query,
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.Date < endDate.Value.Date.AddDays(1));

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
                x.Date,
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
                g.Key.Date,
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
        var query = BuildBaseQuery(userId, includeDeleted, onlyDeleted);
        query = ApplyFilters(query, supplierId, startDate, endDate);

        return await query
            .GroupBy(x => new
            {
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName,
                x.Date,
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
                g.Key.Date,
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
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(userId);
        query = ApplyFilters(query, supplierId, startDate, endDate);

        return await query
            .OrderBy(x => x.Date)
            .Select(x => new UserOrderItem(
                x.OrderId,
                x.UserId,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName ?? string.Empty,
                x.Date,
                x.MenuDate,
                x.Price,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        bool includeDeleted,
        bool onlyDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(userId, includeDeleted, onlyDeleted);
        query = ApplyFilters(query, supplierId, startDate, endDate);

        return await query
            .OrderBy(x => x.Date)
            .Select(x => new UserOrderItem(
                x.OrderId,
                x.UserId,
                x.MealId,
                x.MealName,
                x.SupplierId,
                x.SupplierName ?? string.Empty,
                x.Date,
                x.MenuDate,
                x.Price,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    #endregion
}

#region Helper DTO (internal)

internal sealed class OrderJoinRow
{
    public Guid OrderId { get; init; }
    public string UserId { get; init; } = null!;
    public Guid MealId { get; init; }
    public string MealName { get; init; } = null!;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = null!;
    public DateTime Date { get; init; }
    public DateTime MenuDate { get; init; }
    public decimal Price { get; init; }
    public MealOrderStatus Status { get; init; }
    public bool IsDeleted { get; init; }
}

#endregion
