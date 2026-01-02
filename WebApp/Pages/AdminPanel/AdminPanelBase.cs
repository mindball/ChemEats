using Microsoft.AspNetCore.Components;
using Shared.DTOs.Orders;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace WebApp.Pages.AdminPanel;

public class AdminPanelBase : ComponentBase
{
    // Fallback only if server settings are unavailable
    private readonly decimal _fallbackPortion = 3.00m;

    [Inject] protected IOrderDataService OrderDataService { get; set; } = null!;
    [Inject] protected ISettingsDataService SettingsDataService { get; set; } = null!;

    protected decimal PortionAmount { get; set; }
    protected string? SuccessMessage { get; set; }
    protected string? ErrorMessage { get; set; }

    protected UserOutstandingSummaryDto? PaymentsSummary { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadPortionAsync();
        await LoadPaymentsSummaryAsync();
    }

    protected async Task LoadPortionAsync()
    {
        try
        {
            PortionAmount = await SettingsDataService.GetCompanyPortionAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load portion: {ex.Message}";
            PortionAmount = _fallbackPortion;
        }
    }

    protected async Task LoadPaymentsSummaryAsync()
    {
        try
        {
            PaymentsSummary = await OrderDataService.GetMyPaymentsSummaryAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load payments summary: {ex.Message}";
        }
    }

    protected async Task SaveAsync()
    {
        SuccessMessage = null;
        ErrorMessage = null;

        if (PortionAmount < 0)
        {
            ErrorMessage = "Portion amount must be >= 0.";
            return;
        }

        try
        {
            bool ok = await SettingsDataService.SetCompanyPortionAsync(PortionAmount);
            if (!ok)
            {
                ErrorMessage = "Failed to save portion on server.";
                return;
            }

            SuccessMessage = $"Saved portion amount: {PortionAmount:F2} лв.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save portion: {ex.Message}";
        }
    }

    protected void ResetToDefault()
    {
        PortionAmount = _fallbackPortion;
        SuccessMessage = null;
        ErrorMessage = null;
    }
}