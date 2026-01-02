namespace Shared.DTOs.Orders;

public sealed record UserOutstandingSummaryDto(
    string UserId,
    decimal TotalOutstanding,
    int UnpaidCount
);