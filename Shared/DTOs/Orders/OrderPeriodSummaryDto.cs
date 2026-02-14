namespace Shared.DTOs.Orders;

public sealed record OrderPeriodSummaryDto(
    int TotalOrders,
    int PaidOrders,
    int UnpaidOrders,
    decimal TotalAmount,
    decimal TotalPaid,
    decimal TotalUnpaid
);