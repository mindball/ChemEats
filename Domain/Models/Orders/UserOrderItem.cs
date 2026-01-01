namespace Domain.Models.Orders;


public sealed record UserOrderItem(
    Guid OrderId,
    string UserId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    DateTime MenuDate,
    decimal Price,
    string Status
);