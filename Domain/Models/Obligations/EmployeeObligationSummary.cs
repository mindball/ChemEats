namespace Domain.Models.Obligations;

public sealed record EmployeeObligationSummary(
    string UserId,
    string EmployeeName,
    int UnpaidCount,
    decimal Amount
);
