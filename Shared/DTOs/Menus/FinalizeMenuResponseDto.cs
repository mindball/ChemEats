namespace Shared.DTOs.Menus;

public sealed record FinalizeMenuResponseDto(
    Guid MenuId,
    DateTime FinalizedAt,
    int CompletedOrdersCount,
    decimal TotalAmount
);