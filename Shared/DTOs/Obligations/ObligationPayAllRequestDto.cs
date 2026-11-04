namespace Shared.DTOs.Obligations;

// A null UpToDate pays every unpaid obligation, regardless of its date.
public sealed record ObligationPayAllRequestDto(string UserId, DateTime? UpToDate);
