using Shared.Common.Enums;

namespace Shared.DTOs.Orders;

public sealed record UserOrderPaymentItemDto(
    Guid OrderId,
    Guid MealId,
    string MealName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    decimal Price,
    PaymentStatusDto PaymentStatus,
    DateTime? PaidOn,
    bool PortionApplied,      // New
    decimal PortionAmount,    // New
    decimal NetAmount         // New: Price - PortionAmount (min 0)
);