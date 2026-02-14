namespace Shared.DTOs.Orders;

public sealed record OrderPayRequestDto(string UserId, List<Guid> OrderIds);