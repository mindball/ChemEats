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

    Task<IReadOnlyList<UserOrderItem>> GetOrdersByMenuAsync(
        string userId,
        Guid menuId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderItem>> GetAllOrdersByMenuAsync(
        Guid menuId,
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

    // Null dates mean "no bound": without an end date every unpaid order is included.
    Task<IReadOnlyList<EmployeeOutstandingSummary>> GetOutstandingSummariesAsync(
        DateTime? startDate,
        DateTime? endDate,
        decimal companyPortion,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task MarkAsPaidAsync(
        Guid orderId,
        string userId,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default);

    Task<(int PaidCount, decimal TotalPaid)> MarkOrderAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> orderIds,
        DateTime paidOn,
        decimal companyPortion,
        string paidByUserId,
        CancellationToken cancellationToken = default);

    // A null upToDate pays every unpaid completed order of the user.
    Task<(int PaidCount, decimal TotalPaid)> MarkAllUnpaidAsPaidAsync(
        string userId,
        DateTime? upToDate,
        DateTime paidOn,
        decimal companyPortion,
        string paidByUserId,
        CancellationToken cancellationToken = default);

    Task<(int CompletedCount, decimal TotalAmount)> MarkPendingOrdersAsCompletedForMenuAsync(
        Guid menuId,
        CancellationToken cancellationToken = default);

    // Menus whose ordering window has closed (ActiveUntil <= now) but still have pending orders.
    Task<IReadOnlyList<Guid>> GetExpiredMenuIdsWithPendingOrdersAsync(
        DateTime now,
        CancellationToken cancellationToken = default);

    // Portion each unpaid order is expected to receive when paid: once per user per menu day,
    // on the most expensive order, unless the day already had the portion applied.
    Task<IReadOnlyDictionary<Guid, decimal>> GetExpectedPortionsAsync(
        string userId,
        IReadOnlyCollection<DateTime> menuDates,
        decimal companyPortion,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetPendingOrdersCountByMenuIdsAsync(
        IEnumerable<Guid> menuIds,
        CancellationToken cancellationToken = default);

    Task<bool> HasPortionAppliedOnDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}