using Domain.Entities;

namespace Domain.Models.Obligations;

public sealed record OtherObligationItem(
    Guid Id,
    string UserId,
    string EmployeeName,
    Guid? ObligationTypeId,
    string Description,
    decimal Amount,
    DateTime Date,
    DateTime CreatedAt,
    string? CreatedByName,
    PaymentStatus PaymentStatus,
    DateTime? PaidOn,
    string? PaidByName
);
