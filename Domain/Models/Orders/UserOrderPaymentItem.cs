using Domain.Entities;

namespace Domain.Models.Orders;

public sealed record UserOrderPaymentItem(
    Guid OrderId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    decimal Price,            // Snapshot price
    PaymentStatus PaymentStatus,
    DateTime? PaidOn,
    bool PortionApplied,      // New
    decimal PortionAmount,    // New
    decimal NetAmount         // New: Price - PortionAmount (min 0)
);