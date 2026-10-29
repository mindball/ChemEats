namespace Shared.DTOs.Orders;

public sealed record EmployeeOutstandingSummaryDto(
    string UserId,
    string EmployeeName,
    int UnpaidCount,
    decimal GrossAmount,
    decimal PortionAmount,
    decimal NetAmount,
    int EarlierUnpaidCount = 0,
    decimal EarlierNetAmount = 0m
);
