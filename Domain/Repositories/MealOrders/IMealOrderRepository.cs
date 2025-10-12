using Domain.Entities;

namespace Domain.Repositories.MealOrders;

public interface IMealOrderRepository
{
    Task<MealOrder?> GetByIdAsync(MealOrderId Id, CancellationToken cancellationToken = default);
    Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default);
}