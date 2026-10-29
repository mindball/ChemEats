using Shared.DTOs.Notifications;

namespace WebApp.Services.Notifications;

public interface INotificationDataService
{
    Task<NotificationSettingsDto?> GetMySettingsAsync();
    Task<NotificationSettingsDto?> UpdateMySettingsAsync(NotificationSettingsDto settings);
}
