using Shared.DTOs.Orders;

namespace WebApp.Services.Orders;

public interface IOrderDataService
{
    Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequestDto requestDto);

    Task<List<UserOrderDto>> GetMyOrdersAsync(Guid? supplierId = null, DateTime? startDate = null,
        DateTime? endDate = null);

    Task<bool> DeleteOrderAsync(Guid orderId);

    Task<List<UserOrderItemDto>> GetMyOrderItemsAsync(Guid? supplierId = null, DateTime? startDate = null,  DateTime? endDate = null, bool includeDeleted = false);

    Task<List<UserOrderPaymentItemDto>> GetMyUnpaidPaymentsAsync(Guid? supplierId = null);
    Task<UserOutstandingSummaryDto?> GetMyPaymentsSummaryAsync();
    Task<bool> MarkOrderAsPaidAsync(Guid orderId);
}

public sealed record PlaceOrdersResponse(int Created, List<Guid> Ids);


