namespace Domain.Models.Orders;

public sealed record UserOrderSummary(
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    DateTime MenuDate,
    int Quantity,
    decimal Price,
    IReadOnlyList<Guid> OrderIds,
    string Status,      
    bool IsDeleted      
);