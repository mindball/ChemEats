using Shared.DTOs.Orders;

namespace WebApp.Services.Orders;

public interface IOrderDataService
{
    Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequest request);
}

public sealed record PlaceOrdersResponse(int Created, List<Guid> Ids);


