using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public interface IEmployeeExternalService
{
    Task<List<UserDto>> GetAllEmployeesAsync();
    Task<UserDto?> GetEmployeeByAbbreviationAsync(string abbreviation);
}