namespace Shared.DTOs.Orders;

public sealed record EmployeeOutstandingSummaryDto(
    string UserId,
    string EmployeeName,
    int UnpaidCount,
    decimal GrossAmount,
    decimal PortionAmount,
    decimal NetAmount,
    int EarlierUnpaidCount = 0,
    decimal EarlierNetAmount = 0m,
    int OtherUnpaidCount = 0,       // Unpaid other obligations up to the period end (no lower bound)
    decimal OtherAmount = 0m
);
