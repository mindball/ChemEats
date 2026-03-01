using System.Net.Http.Json;
using Shared;

namespace WebApp.Services.Settings;

public class SettingsDataService : ISettingsDataService
{
    private readonly HttpClient _httpClient;

    public SettingsDataService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<decimal> GetCompanyPortionAsync()
    {
        var resp = await _httpClient.GetFromJsonAsync<PortionResponse>($"{ApiRoutes.Settings.Base}/{ApiRoutes.Settings.Portion}");
        return resp?.PortionAmount ?? 0m;
    }

    public async Task<bool> SetCompanyPortionAsync(decimal portionAmount)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Settings.Base}/{ApiRoutes.Settings.Portion}", new PortionRequest(portionAmount));
        return response.IsSuccessStatusCode;
    }

    private sealed record PortionResponse(decimal PortionAmount);
    private sealed record PortionRequest(decimal PortionAmount);
}