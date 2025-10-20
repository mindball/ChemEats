using Domain.Entities;

namespace Domain.Repositories.MealOrders;

public interface IMealOrderRepository
{
    Task<MealOrder?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default);
    Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default);
}