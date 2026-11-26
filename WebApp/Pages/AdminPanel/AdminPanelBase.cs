using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.AdminPanel;

public class AdminPanelBase : LocalizedComponentBase
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

    // Unpaid completed orders of all employees, regardless of the menu date.
    protected List<EmployeeOutstandingSummaryDto>? OutstandingEmployees { get; private set; }

    protected int OutstandingOrderCount =>
        OutstandingEmployees?.Sum(s => s.UnpaidCount + s.EarlierUnpaidCount) ?? 0;

    protected decimal OutstandingTotalDue =>
        OutstandingEmployees?.Sum(s => s.NetAmount + s.EarlierNetAmount) ?? 0m;

    // User management state
    protected List<EmployeeDto>? AllUsers { get; private set; }
    protected string? UserSearchTerm { get; set; }
    protected string? UserManagementError { get; set; }
    protected string? UserManagementSuccess { get; set; }
    protected bool IsLoadingUsers { get; private set; }
    protected bool IsCreatingEmployee { get; private set; }
    protected string? NewEmployeeAbbreviation { get; set; }
    protected string? NewEmployeeFullName { get; set; }
    protected string? NewEmployeeEmail { get; set; }

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
            ErrorMessage = L["Failed to load portion: {0}", ex.Message];
            PortionAmount = _fallbackPortion;
        }
    }

    protected async Task LoadPaymentsSummaryAsync()
    {
        try
        {
            List<EmployeeOutstandingSummaryDto>? outstanding = await OrderDataService.GetAllOutstandingAsync();

            if (outstanding is null)
            {
                ErrorMessage = L["Failed to load outstanding payments."];
                return;
            }

            OutstandingEmployees = outstanding
                .OrderByDescending(s => s.NetAmount + s.EarlierNetAmount)
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to load outstanding payments: {0}", ex.Message];
        }
    }

    protected async Task SaveAsync()
    {
        SuccessMessage = null;
        ErrorMessage = null;

        if (PortionAmount < 0)
        {
            ErrorMessage = L["Portion amount must be >= 0."];
            return;
        }

        try
        {
            bool ok = await SettingsDataService.SetCompanyPortionAsync(PortionAmount);
            if (!ok)
            {
                ErrorMessage = L["Failed to save portion on server."];
                return;
            }

            SuccessMessage = L["Saved portion amount: {0:F2} €", PortionAmount];
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to save portion: {0}", ex.Message];
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
            UserManagementError = L["Failed to load users: {0}", ex.Message];
        }
        finally
        {
            IsLoadingUsers = false;
        }
    }

    protected async Task CreateEmployeeAsync()
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        string abbreviation = NewEmployeeAbbreviation?.Trim().ToUpperInvariant() ?? string.Empty;
        string fullName = NewEmployeeFullName?.Trim() ?? string.Empty;

        if (abbreviation.Length == 0 || fullName.Length == 0)
        {
            UserManagementError = L["Abbreviation and full name are required."];
            return;
        }

        try
        {
            IsCreatingEmployee = true;
            string? email = string.IsNullOrWhiteSpace(NewEmployeeEmail) ? null : NewEmployeeEmail.Trim();
            (TemporaryPasswordDto? created, string? error) = await EmployeeDataService.CreateEmployeeAsync(
                new CreateEmployeeRequestDto(abbreviation, fullName, email));

            if (created is null)
            {
                UserManagementError = error ?? L["Failed to create employee."];
                return;
            }

            UserManagementSuccess =
                L["Employee '{0}' created. Temporary password: {1} (shown only once; the employee must change it at first login).", created.Abbreviation, created.TemporaryPassword];
            NewEmployeeAbbreviation = null;
            NewEmployeeFullName = null;
            NewEmployeeEmail = null;
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            UserManagementError = L["Error creating employee: {0}", ex.Message];
        }
        finally
        {
            IsCreatingEmployee = false;
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
                UserManagementSuccess = L["Role '{0}' assigned successfully.", roleName];
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = L["Failed to assign role."];
            }
        }
        catch (Exception ex)
        {
            UserManagementError = L["Error assigning role: {0}", ex.Message];
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
                UserManagementSuccess = L["Role '{0}' removed successfully.", roleName];
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = L["Failed to remove role."];
            }
        }
        catch (Exception ex)
        {
            UserManagementError = L["Error removing role: {0}", ex.Message];
        }
    }

    protected async Task ResetUserPasswordAsync(EmployeeDto user)
    {
        UserManagementError = null;
        UserManagementSuccess = null;

        try
        {
            TemporaryPasswordDto? reset = await EmployeeDataService.ResetPasswordAsync(user.UserId);
            if (reset is not null)
            {
                UserManagementSuccess =
                    L["Password for '{0}' reset. Temporary password: {1} (shown only once; the employee must change it at next login).", reset.Abbreviation, reset.TemporaryPassword];
                await LoadUsersAsync();
            }
            else
            {
                UserManagementError = L["Failed to reset password."];
            }
        }
        catch (Exception ex)
        {
            UserManagementError = L["Error resetting password: {0}", ex.Message];
        }
    }

    protected async Task<string?> GetSelectedRoleAsync(string userId)
    {
        return await JsRuntime.InvokeAsync<string?>(
            "eval",
            $"document.getElementById('role-{userId}')?.value || null");
    }
}