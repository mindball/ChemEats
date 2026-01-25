namespace Shared.DTOs.Orders;

public sealed record UserOrderDto(
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