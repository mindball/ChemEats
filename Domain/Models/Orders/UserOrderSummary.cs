namespace Domain.Models.Orders;

public sealed record UserOrderSummary(
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    int Quantity,
    decimal Price
);