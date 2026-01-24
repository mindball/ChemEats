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
            string? baseAddress = _httpClient.BaseAddress?.ToString() ?? "Unknown";
            _logger.LogInformation(
                "Fetching all employees from external API: {BaseAddress}Employees",
                baseAddress);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            HttpResponseMessage response = await _httpClient.GetAsync("Employees");

            sw.Stop();

            _logger.LogInformation(
                "HTTP GET Employees completed with status {StatusCode} in {ElapsedMs} ms",
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            response.EnsureSuccessStatusCode();

            List<UserDto>? employees = await response.Content.ReadFromJsonAsync<List<UserDto>>();
            int count = employees?.Count ?? 0;

            _logger.LogInformation(
                "Successfully fetched {EmployeeCount} employees from external API",
                count);

            return employees ?? new List<UserDto>();
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx,
                "HTTP request failed when fetching employees from external API: {StatusCode} - {ErrorMessage}",
                httpEx.StatusCode,
                httpEx.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch employees from external API: {ErrorMessage}",
                ex.Message);
            return [];
        }
    }

    public async Task<UserDto?> GetEmployeeByAbbreviationAsync(string abbreviation)
    {
        try
        {
            _logger.LogInformation(
                "Fetching employee by abbreviation: {Abbreviation}",
                abbreviation);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            HttpResponseMessage response = await _httpClient.GetAsync($"Employee?Abbreviation={abbreviation}");
            
            sw.Stop();

            _logger.LogInformation(
                "HTTP GET Employee?Abbreviation={Abbreviation} completed with status {StatusCode} in {ElapsedMs} ms",
                abbreviation,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            response.EnsureSuccessStatusCode();

            UserDto? employee = await response.Content.ReadFromJsonAsync<UserDto>();

            if (employee != null)
            {
                _logger.LogInformation(
                    "Employee {Abbreviation} found: {Name} ({Code})",
                    abbreviation,
                    employee.Name,
                    employee.Code);
            }
            else
            {
                _logger.LogWarning(
                    "Employee {Abbreviation} not found in external API",
                    abbreviation);
            }

            return employee;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx,
                "HTTP request failed when fetching employee {Abbreviation}: {StatusCode} - {ErrorMessage}",
                abbreviation,
                httpEx.StatusCode,
                httpEx.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch employee {Abbreviation}: {ErrorMessage}",
                abbreviation,
                ex.Message);
            return null;
        }
    }
}