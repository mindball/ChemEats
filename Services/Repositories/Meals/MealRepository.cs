using Domain;
using Domain.Entities;
using Domain.Repositories.Meals;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Meals;

public class MealRepository : IMealRepository
{
    private readonly AppDbContext _dbContext;

    public MealRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Meal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.Meals
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task AddAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(meal);

        await _dbContext.Meals.AddAsync(meal, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

