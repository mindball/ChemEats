namespace Shared.DTOs.Obligations;

public sealed record ObligationPayRequestDto(string UserId, List<Guid> ObligationIds);
