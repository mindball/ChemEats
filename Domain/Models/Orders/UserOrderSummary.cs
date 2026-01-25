namespace Domain.Models.Orders;

public sealed record UserOrderSummary(
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderedAt,
    DateTime MenuDate,
    int Quantity,
    decimal Price,
    IReadOnlyList<Guid> OrderIds,
    string Status,      
    bool IsDeleted      
);