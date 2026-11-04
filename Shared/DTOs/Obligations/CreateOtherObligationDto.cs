namespace Shared.DTOs.Obligations;

public sealed record CreateOtherObligationDto(string UserId, string Description, decimal Amount, DateTime Date);
