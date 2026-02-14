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

    Task SoftDeleteAsync(Guid orderId, CancellationToken cancellationToken = default);

        
    Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeDeleted = false,
        MealOrderStatus? status = null,
        CancellationToken cancellationToken = default);

    //when create admin panel then admin must see delete orders 
    Task<IReadOnlyList<UserOrderSummary>> GetUserOrdersAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeDeleted = false,
        bool onlyDeleted = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderItem>> GetUserOrderItemsAsync(
        string userId,
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeDeleted = false,
        bool onlyDeleted = false,
        MealOrderStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<UserOutstandingSummary> GetUserOutstandingSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderPaymentItem>> GetUnpaidOrdersAsync(
        string userId,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderPaymentItem>> GetAllOrdersForPeriodAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);

    Task MarkAsPaidAsync(
        Guid orderId,
        string userId,
        DateTime paidOn,
        CancellationToken cancellationToken = default);

    Task<(int PaidCount, decimal TotalPaid)> MarkOrderAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> orderIds,
        DateTime paidOn,
        CancellationToken cancellationToken = default);

    Task<bool> HasPortionAppliedOnDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}