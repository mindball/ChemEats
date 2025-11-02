using Domain;
using Domain.Entities;
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
}