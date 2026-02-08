using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace WebApp.Pages.Orders;

public class MyOrdersBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }
    protected List<UserOrderItemDto>? Orders { get; private set; }

    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected Guid? SelectedSupplierId { get; set; }
    protected string? SelectedStatus { get; set; }
    protected bool IncludeDeleted { get; set; }

    protected string? ErrorMessage { get; private set; }
    protected bool IsLoading { get; private set; }

    protected IEnumerable<string> AvailableStatuses =>
    [
        "All",
        "Pending",
        "Completed",
        "Cancelled"
    ];

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            AuthenticationState auth = await AuthenticationStateTask;
            ClaimsPrincipal? user = auth.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                ErrorMessage = "You must be signed in to view your orders.";
                return;
            }

            Menus = (await MenuDataService.GetAllMenusAsync(true)).ToList();

            StartDate = DateTime.Today;
            EndDate = DateTime.Today;
            if (Menus!.All(m => m.Date.Date != StartDate.Value.Date))
            {
                StartDate = null;
                EndDate = null;
            }

            SelectedStatus = "All";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load orders: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
            {
                (StartDate, EndDate) = (EndDate, StartDate);
            }

            string? statusFilter = SelectedStatus == "All" ? null : SelectedStatus;

            Orders = await OrderDataService.GetMyOrderItemsAsync(
                SelectedSupplierId, 
                StartDate, 
                EndDate, 
                IncludeDeleted,
                statusFilter);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load orders: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected static string FormatBulgarianDate(DateTime date)
    {
        CultureInfo bgCulture = new("bg-BG");
        string dayName = date.ToString("dddd", bgCulture);
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName[1..];
        return $"{capitalizedDay} {date:dd.MM.yyyy'г.'}";
    }

    protected IEnumerable<(Guid SupplierId, string SupplierName)> AvailableSuppliers =>
        Menus?.Select(m => (m.SupplierId, m.SupplierName))
            .Distinct()
            .OrderBy(x => x.SupplierName) ?? Enumerable.Empty<(Guid, string)>();
}