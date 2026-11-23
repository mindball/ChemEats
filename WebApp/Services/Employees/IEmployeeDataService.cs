using Shared.DTOs.Employees;

namespace WebApp.Services.Employees;

public interface IEmployeeDataService
{
    Task<List<EmployeeDto>> GetAllEmployeesAsync();
    Task<(TemporaryPasswordDto? Created, string? Error)> CreateEmployeeAsync(CreateEmployeeRequestDto request);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    Task<(bool Success, string? Error)> ChangeMyPasswordAsync(ChangePasswordRequestDto request);
    Task<TemporaryPasswordDto?> ResetPasswordAsync(string userId);
}