namespace Shared.DTOs.Obligations;

public sealed record UpdateOtherObligationDto(Guid ObligationTypeId, decimal Amount, DateTime Date);
