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
    decimal Price,          // Snapshot price at order time
    string Status,
    bool PortionApplied,    // New
    decimal PortionAmount,  // New
    decimal NetAmount       // New: Price - Portion (min 0)
);