namespace Shared.DTOs.Orders;

public sealed record UserOrderDto(
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    int Quantity,
    decimal Price
);