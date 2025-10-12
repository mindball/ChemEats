using Domain.Entities;

namespace Domain.Repositories.Meals;

public interface IMealRepository
{
    Task<Meal?> GetByIdAsync(MealId Id, CancellationToken cancellationToken = default);
    Task AddAsync(Meal meal, CancellationToken cancellationToken = default);
}