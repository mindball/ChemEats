namespace Shared.DTOs.Employees;

public sealed record TemporaryPasswordDto(
    string UserId,
    string Abbreviation,
    string TemporaryPassword);
