using System.Net.Http.Json;
using Shared;
using Shared.DTOs.Reports;

namespace WebApp.Services.Menus;

public sealed class MenuReportService : IMenuReportService
{
    private readonly HttpClient _httpClient;

    public MenuReportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MenuReportDto?> GetMenuReportAsync(
        Guid menuId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(
            ApiRoutes.Reports.MenuReportData(menuId),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MenuReportDto>(cancellationToken);
    }
}
