using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public class EmployeeExternalService : IEmployeeExternalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployeeExternalService> _logger;

    public EmployeeExternalService(HttpClient httpClient, ILogger<EmployeeExternalService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllEmployeesAsync()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync("Employees");
            response.EnsureSuccessStatusCode();
            List<UserDto>? employees = await response.Content.ReadFromJsonAsync<List<UserDto>>();
            return employees ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch employees from external API.");
            return [];
        }
    }

    public async Task<UserDto?> GetEmployeeByAbbreviationAsync(string abbreviation)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"Employee?Abbreviation={abbreviation}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch employee {Abbreviation}", abbreviation);
            return null;
        }
    }
}