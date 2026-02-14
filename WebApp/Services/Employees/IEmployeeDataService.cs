using Shared.DTOs.Employees;

namespace WebApp.Services.Employees;

public interface IEmployeeDataService
{
    Task<List<EmployeeDto>> GetAllEmployeesAsync();
}