namespace Shared.DTOs.Obligations;

public sealed record CreateOtherObligationDto(string UserId, Guid ObligationTypeId, decimal Amount, DateTime Date);
