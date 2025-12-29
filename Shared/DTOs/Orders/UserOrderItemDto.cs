namespace Shared.DTOs.Orders;

/// <summary>
/// Represents a single item in a user's order, including meal, supplier, and order details.
/// </summary>
/// <param name="OrderId">The unique identifier for the order.</param>
/// <param name="UserId">The unique identifier for the user who placed the order.</param>
/// <param name="MealId">The unique identifier for the meal.</param>
/// <param name="MealName">The name of the meal.</param>
/// <param name="SupplierId">The unique identifier for the supplier.</param>
/// <param name="SupplierName">The name of the supplier.</param>
/// <param name="Date">The date and time when the order was placed.</param>
/// <param name="Price">The price of the meal in the order.</param>
/// <param name="Status">The current status of the order item.</param>
public sealed record UserOrderItemDto(
    Guid OrderId,
    string UserId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    decimal Price,
    string Status,
    bool IsDeleted
);