using System.Net.Http.Json;
using Shared;
using Shared.DTOs.Notifications;

namespace WebApp.Services.Notifications;

public class NotificationDataService : INotificationDataService
{
    private readonly HttpClient _httpClient;

    public NotificationDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NotificationSettingsDto?> GetMySettingsAsync()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Notifications.MySettings());
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<NotificationSettingsDto>();
    }

    public async Task<NotificationSettingsDto?> UpdateMySettingsAsync(NotificationSettingsDto settings)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(ApiRoutes.Notifications.MySettings(), settings);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<NotificationSettingsDto>();
    }
}
