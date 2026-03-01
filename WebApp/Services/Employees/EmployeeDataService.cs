using System.Net.Http.Json;
using Shared;
using Shared.DTOs.Employees;

namespace WebApp.Services.Employees;

public class EmployeeDataService : IEmployeeDataService
{
    private readonly HttpClient _httpClient;

    public EmployeeDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EmployeeDto>> GetAllEmployeesAsync()
    {
        List<EmployeeDto>? employees = await _httpClient.GetFromJsonAsync<List<EmployeeDto>>(ApiRoutes.Employees.Base);
        return employees ?? [];
    }
}