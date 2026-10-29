using Domain.Entities;

namespace Domain.Models.Orders;

public sealed record UserOrderPaymentItem(
    Guid OrderId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderedAt,
    DateTime MenuDate,
    decimal Price,            // Snapshot price
    PaymentStatus PaymentStatus,
    DateTime? PaidOn,
    bool PortionApplied,      // New
    decimal PortionAmount,    // New
    decimal NetAmount,        // New: Price - PortionAmount (min 0)
    bool IsCancelled,
    bool IsCompleted,         // Menu finalized; only completed orders can be paid
    string? PaidByName
);