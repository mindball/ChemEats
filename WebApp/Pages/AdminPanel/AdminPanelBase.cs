using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace WebApp.Pages.AdminPanel;

public class AdminPanelBase : ComponentBase
{
    // Fallback only if server settings are unavailable
    private readonly decimal _fallbackPortion = 3.00m;

    protected static readonly string[] AvailableRoles = ["Admin", "Employee", "Manager", "Supervisor"];

    [Inject] protected IOrderDataService OrderDataService { get; set; } = null!;
    [Inject] protected ISettingsDataService SettingsDataService { get; set; } = null!;
    [Inject] protected IEmployeeDataService EmployeeDataService { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;

    protected decimal PortionAmount { get; set; }
    protected string? SuccessMessage { get; set; }
    protected string? ErrorMessage { get; set; }

    protected UserOutstandingSummaryDto? PaymentsSummary { get; private set; }

    // User management state
    protected List<EmployeeDto>? AllUsers { get; private set; }
    protected string? UserSearchTerm { get; set; }
    protected string? UserManagementError { get; set; }
    protected string? UserManagementSuccess { get; set; }
    protected bool IsLoadingUsers { get; private set; }
    protected bool IsSyncingEmployees { get; private set; }

    protected List<EmployeeDto> FilteredUsers
    {
        get
        {
            if (AllUsers is null)
                return [];

            if (string.IsNullOrWhiteSpace(UserSearchTerm))
                return AllUsers;

            string searchLower = UserSearchTerm.ToLowerInvariant();

            return AllUsers
                .Where(u =>
                    u.FullName.ToLowerInvariant().Contains(searchLower) ||
                    u.Abbreviation.ToLowerInvariant().Contains(searchLower) ||
                    (u.Email?.ToLowerInvariant().Contains(searchLower) ?? false))
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPortionAsync();
        await LoadPaymentsSummaryAsync();
        await LoadUsersAsync();
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

    protected async Task LoadUsersAsync()
    {
        try
        {
            IsLoadingUsers = true;
            UserManagementError = null;
            AllUsers = await EmployeeDataService.GetAllEmployeesAsync();
        }
        catch (Exception ex)
        {
            UserManagementError = $"Failed to load users: {ex.Message}";
        }
        finally
        {
            IsLoadingUsers = false;
        }
    }

    protected async Task SyncEmployeesAsync()
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        try
        {
            IsSyncingEmployees = true;
            bool result = await EmployeeDataService.SyncEmployeesAsync();

            if (!result)
            {
                UserManagementError = "Failed to synchronize employees.";
                return;
            }

            UserManagementSuccess = "Employees synchronized successfully.";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            UserManagementError = $"Error synchronizing employees: {ex.Message}";
        }
        finally
        {
            IsSyncingEmployees = false;
        }
    }

    protected async Task AssignRoleAsync(string userId, string roleName)
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        try
        {
            bool result = await EmployeeDataService.AssignRoleAsync(userId, roleName);
            if (result)
            {
                UserManagementSuccess = $"Role '{roleName}' assigned successfully.";
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = "Failed to assign role.";
            }
        }
        catch (Exception ex)
        {
            UserManagementError = $"Error assigning role: {ex.Message}";
        }
    }

    protected async Task RemoveRoleAsync(string userId, string roleName)
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        try
        {
            bool result = await EmployeeDataService.RemoveRoleAsync(userId, roleName);
            if (result)
            {
                UserManagementSuccess = $"Role '{roleName}' removed successfully.";
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = "Failed to remove role.";
            }
        }
        catch (Exception ex)
        {
            UserManagementError = $"Error removing role: {ex.Message}";
        }
    }

    protected async Task ResetUserPasswordAsync(EmployeeDto user)
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        try
        {
            bool result = await EmployeeDataService.ResetPasswordAsync(user.UserId);
            if (result)
            {
                UserManagementSuccess = $"Password for '{user.Abbreviation}' reset to abbreviation.";
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = "Failed to reset password.";
            }
        }
        catch (Exception ex)
        {
            UserManagementError = $"Error resetting password: {ex.Message}";
        }
    }

    protected async Task<string?> GetSelectedRoleAsync(string userId)
    {
        return await JsRuntime.InvokeAsync<string?>(
            "eval",
            $"document.getElementById('role-{userId}')?.value || null");
    }
}