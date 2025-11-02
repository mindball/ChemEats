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

    public async Task<MealOrder?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.MealOrders
            .AsNoTracking()
            .Include(mo => mo.Meal)
            .Include(mo => mo.User)
            .FirstOrDefaultAsync(mo => mo.Id == Id, cancellationToken);
    }

    public async Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mealOrder);

        cancellationToken.ThrowIfCancellationRequested();

        bool mealExists = await _dbContext.Meals
            .AsNoTracking()
            .AnyAsync(m => m.Id == mealOrder.MealId, cancellationToken);

        if (!mealExists)
            throw new ArgumentException($"Meal with id '{mealOrder.MealId}' does not exist.", nameof(mealOrder));

        // Attach the user only if it's set (optional safeguard)
        if (mealOrder.User != null)
            _dbContext.Attach(mealOrder.User);

        await _dbContext.MealOrders.AddAsync(mealOrder, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderSummary>> GetUserOrdersAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(userId);

        var query =
            from mo in _dbContext.MealOrders.AsNoTracking()
            join menu in _dbContext.Menus.AsNoTracking() on mo.Date.Date equals menu.Date.Date
            join supplier in _dbContext.Suppliers.AsNoTracking() on menu.SupplierId equals supplier.Id
            from menuMeal in menu.Meals
            where mo.UserId == userId && menuMeal.Id == mo.MealId
            select new
            {
                MealId = menuMeal.Id,
                MealName = menuMeal.Name,
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Date = mo.Date,
                Price = menuMeal.Price.Amount
            };

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.Date.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.Date.Date <= endDate.Value.Date);

        var grouped = await query
            .GroupBy(x => new { x.MealId, x.MealName, x.SupplierId, x.SupplierName, x.Date, x.Price })
            .Select(g => new UserOrderSummary(
                g.Key.MealId,
                g.Key.MealName,
                g.Key.SupplierId,
                g.Key.SupplierName ?? string.Empty,
                g.Key.Date,
                g.Count(),
                g.Key.Price
            ))
            .ToListAsync(cancellationToken);

        return grouped;
    }
}