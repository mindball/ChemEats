using Shared.Common.Enums;

namespace Shared.DTOs.Obligations;

public sealed record OtherObligationDto(
    Guid Id,
    string UserId,
    string EmployeeName,
    Guid? ObligationTypeId,
    string Description,
    decimal Amount,
    DateTime Date,
    DateTime CreatedAt,
    string? CreatedByName,
    PaymentStatusDto PaymentStatus,
    DateTime? PaidOn,
    string? PaidByName
);
