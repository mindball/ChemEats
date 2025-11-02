using Shared.DTOs.Orders;

namespace WebApp.Services.Orders;

public interface IOrderDataService
{
    Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequestDto requestDto);

    Task<List<UserOrderDto>> GetMyOrdersAsync(Guid? supplierId = null, DateTime? startDate = null,
        DateTime? endDate = null);
}

public sealed record PlaceOrdersResponse(int Created, List<Guid> Ids);


