using System.Net.Http.Json;

namespace WebApp.Services.Settings;

public class SettingsDataService : ISettingsDataService
{
    private readonly HttpClient _httpClient;

    public SettingsDataService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<decimal> GetCompanyPortionAsync()
    {
        var resp = await _httpClient.GetFromJsonAsync<PortionResponse>("api/settings/portion");
        return resp?.PortionAmount ?? 0m;
    }

    public async Task<bool> SetCompanyPortionAsync(decimal portionAmount)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings/portion", new PortionRequest(portionAmount));
        return response.IsSuccessStatusCode;
    }

    private sealed record PortionResponse(decimal PortionAmount);
    private sealed record PortionRequest(decimal PortionAmount);
}