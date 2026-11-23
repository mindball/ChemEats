namespace Shared.DTOs.Employees;

public sealed record CreateEmployeeRequestDto(
    string Abbreviation,
    string FullName,
    string? Email);
