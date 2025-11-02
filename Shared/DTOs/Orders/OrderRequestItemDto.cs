namespace Shared.DTOs.Orders;

public sealed record OrderRequestItemDto(Guid MealId, DateTime Date, int Quantity);

public sealed record PlaceOrdersRequestDto(List<OrderRequestItemDto> Items);