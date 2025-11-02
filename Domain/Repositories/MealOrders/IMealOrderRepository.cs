using Domain.Entities;
using Domain.Models.Orders;

namespace Domain.Repositories.MealOrders;

public interface IMealOrderRepository
{
    Task<MealOrder?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default);
    Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserOrderSummary>> GetUserOrdersAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? date = null,
        CancellationToken cancellationToken = default);
}