namespace Shared.DTOs.Orders;


public sealed record UserOrderItemDto(
    Guid OrderId,
    string UserId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    decimal Price,
    string Status
);