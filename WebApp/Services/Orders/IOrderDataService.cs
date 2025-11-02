using Shared.DTOs.Orders;

namespace WebApp.Services.Orders;

public interface IOrderDataService
{
    Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequestDto requestDto);
}

public sealed record PlaceOrdersResponse(int Created, List<Guid> Ids);


