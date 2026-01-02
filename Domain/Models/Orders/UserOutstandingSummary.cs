namespace Domain.Models.Orders;

public record UserOutstandingSummary(
    string UserId,
    decimal TotalOutstanding,
    int UnpaidCount
);