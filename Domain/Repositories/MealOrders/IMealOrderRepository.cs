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
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}