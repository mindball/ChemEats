namespace Shared.DTOs.Orders;

// A null UpToDate pays every unpaid completed order, regardless of its menu date.
public sealed record PayAllUnpaidRequestDto(string UserId, DateTime? UpToDate);
