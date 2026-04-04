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

    public async Task<bool> SyncEmployeesAsync()
    {
        HttpResponseMessage response = await _httpClient.PostAsync(ApiRoutes.Employees.SyncEmployees, null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignRoleAsync(string userId, string roleName)
    {
        HttpResponseMessage response = await _httpClient.PostAsync(
            ApiRoutes.Employees.RoleAction(userId, roleName), null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleName)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(
            ApiRoutes.Employees.RoleAction(userId, roleName));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangeMyPasswordAsync(ChangePasswordRequestDto request)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            ApiRoutes.Employees.ChangeMyPassword(),
            request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResetPasswordAsync(string userId)
    {
        HttpResponseMessage response = await _httpClient.PostAsync(
            ApiRoutes.Employees.ResetPassword(userId),
            null);
        return response.IsSuccessStatusCode;
    }
}