using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Shared.DTOs.Notifications;
using WebApp.Services.Notifications;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Accounts;

public class NotificationSettingsBase : LocalizedComponentBase
{
    [Inject] protected INotificationDataService NotificationDataService { get; set; } = null!;

    protected NotificationSettingsModel Model { get; set; } = new();
    protected bool IsLoading { get; private set; }
    protected bool IsSaving { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            NotificationSettingsDto? settings = await NotificationDataService.GetMySettingsAsync();
            if (settings is null)
            {
                ErrorMessage = L["Could not load your notification settings."];
                return;
            }

            ApplySettings(settings);
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Could not load your notification settings: {0}", ex.Message];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        string? email = string.IsNullOrWhiteSpace(Model.Email) ? null : Model.Email.Trim();
        if (Model.RemindersEnabled && email is null)
        {
            ErrorMessage = L["Enter an email address to receive order reminders."];
            return;
        }

        // The server performs the strict check; this only catches obvious typos early.
        if (email is not null && (email.Length > 256 || !new EmailAddressAttribute().IsValid(email)))
        {
            ErrorMessage = L["Please enter a valid email address."];
            return;
        }

        IsSaving = true;

        try
        {
            NotificationSettingsDto? saved = await NotificationDataService.UpdateMySettingsAsync(
                new NotificationSettingsDto(email, Model.RemindersEnabled));

            if (saved is null)
            {
                ErrorMessage = L["Saving failed. Please check the email address and try again."];
                return;
            }

            ApplySettings(saved);
            SuccessMessage = L["Notification settings saved."];
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Saving failed: {0}", ex.Message];
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ApplySettings(NotificationSettingsDto settings)
    {
        Model = new NotificationSettingsModel
        {
            Email = settings.NotificationEmail ?? string.Empty,
            RemindersEnabled = settings.OrderRemindersEnabled
        };
    }

    public sealed class NotificationSettingsModel
    {
        public string Email { get; set; } = string.Empty;

        public bool RemindersEnabled { get; set; }
    }
}
