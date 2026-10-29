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

            // Default: current month, the same period the payments are made for
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = StartDate.Value.AddMonths(1).AddDays(-1);

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

    // Cancelled and deleted orders are never paid, so they do not count towards the totals.
    protected IEnumerable<UserOrderItemDto> PayableOrders =>
        Orders?.Where(o => !o.IsDeleted && !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        ?? [];

    protected decimal TotalPortion =>
        PayableOrders.Sum(o => (o.PortionApplied ? o.PortionAmount : 0m) + o.ExpectedPortionAmount);

    protected decimal TotalNet => PayableOrders.Sum(GetNetAmount);

    protected int PaidCount => PayableOrders.Count(o => o.IsPaid);
    protected decimal TotalPaid => PayableOrders.Where(o => o.IsPaid).Sum(GetNetAmount);

    protected int RemainingCount => PayableOrders.Count(o => !o.IsPaid);
    protected decimal TotalRemaining => PayableOrders.Where(o => !o.IsPaid).Sum(GetNetAmount);

    // Unpaid orders show the portion they will receive at payment.
    protected static decimal GetNetAmount(UserOrderItemDto item) =>
        Math.Max(0m, item.NetAmount - item.ExpectedPortionAmount);

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