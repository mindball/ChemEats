using Domain.Entities;
using Domain.Repositories.Meals;

namespace Services.Repositories.Meals;

public class MealRepository : IMealRepository
{
    public Task<Meal?> GetByIdAsync(MealId Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

