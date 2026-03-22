using Shared.DTOs.Employees;

namespace WebApp.Services.Employees;

public interface IEmployeeDataService
{
    Task<List<EmployeeDto>> GetAllEmployeesAsync();
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    Task<bool> ChangeMyPasswordAsync(ChangePasswordRequestDto request);
    Task<bool> ResetPasswordAsync(string userId);
}