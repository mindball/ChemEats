namespace Shared.DTOs.Employees;

public sealed record EmployeeDto(
    string UserId,
    string FullName,
    string Email,
    string Abbreviation
);