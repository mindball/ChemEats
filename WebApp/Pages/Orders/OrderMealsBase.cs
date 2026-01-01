using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace WebApp.Pages.Orders;

public class OrderMealsBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }

    // Supplier filter: store selected supplier id (optional) and list of suppliers present in menus
    protected Guid? SelectedSupplierId { get; set; }
    protected IReadOnlyList<(Guid Id, string Name)> SuppliersForFilter { get; private set; } = [];

    protected IEnumerable<MenuDto> FilteredMenus => Menus is null
        ? []
        : Menus
            .Where(m => !FilterDate.HasValue || m.Date.Date == FilterDate.Value.Date)
            .Where(m => !SelectedSupplierId.HasValue || m.SupplierId == SelectedSupplierId.Value);

    // Always default to tomorrow; user can change to any future date
    protected DateTime? FilterDate { get; set; }

    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }
    protected bool IsLoading { get; private set; }

    protected bool IsAuthenticated { get; private set; }
    protected string? CurrentUserName { get; private set; }

    protected readonly Dictionary<(Guid MealId, DateTime Date), int> Selected = new();
    protected int SelectedItemsCount => Selected.Values.Sum();
    protected bool CanPlaceOrder => IsAuthenticated && SelectedItemsCount > 0;

    protected List<UserOrderItemDto> MyOrders { get; private set; } = [];

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            AuthenticationState authState = await AuthenticationStateTask;
            ClaimsPrincipal? user = authState.User;
            IsAuthenticated = user?.Identity?.IsAuthenticated == true;
            CurrentUserName = user?.Identity?.Name;

            Menus = (await MenuDataService.GetAllMenusAsync(includeDeleted: false)).ToList();

            // Default filter date to tomorrow
            FilterDate = DateTime.Today.AddDays(1);

            // Build supplier list from menus
            SuppliersForFilter = Menus
                .GroupBy(m => (m.SupplierId, m.SupplierName))
                .Select(g => (g.Key.SupplierId, g.Key.SupplierName))
                .OrderBy(x => x.SupplierName)
                .ToList();

            await LoadMyOrdersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load menus: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task LoadMyOrdersAsync()
    {
        try
        {
            // show only from today and onwards
            DateTime start = DateTime.Today;
            MyOrders = (await OrderDataService.GetMyOrderItemsAsync(null, start, null)).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load your orders: {ex.Message}";
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    protected static string FormatBulgarianDate(DateTime date)
    {
        CultureInfo bgCulture = new("bg-BG");
        string dayName = date.ToString("dddd", bgCulture);
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName.Substring(1);
        return $"{capitalizedDay} {date:dd.MM.yyyy'ã.'}";
    }

    protected bool IsSelected((Guid MealId, DateTime Date) key)
    {
        return Selected.ContainsKey(key);
    }

    // Default quantity is now 0 for items not in Selected
    protected int GetQuantity((Guid MealId, DateTime Date) key)
    {
        return Selected.GetValueOrDefault(key, 0);
    }

    protected void ToggleSelection((Guid MealId, DateTime Date) key, bool isSelected)
    {
        if (isSelected)
        {
            // add with a sensible default of 1 when user explicitly selects via checkbox
            Selected.TryAdd(key, 1);
        }
        else
        {
            Selected.Remove(key);
        }
    }

    // Accept 0 as "remove item" and allow adding/updating quantities > 0
    protected void UpdateQuantity((Guid MealId, DateTime Date) key, string value)
    {
        if (!int.TryParse(value, out int q) || q < 0)
            q = 0;

        if (q == 0)
        {
            Selected.Remove(key);
        }
        else
        {
            if (!Selected.TryAdd(key, q))
                Selected[key] = q;
        }
    }

    protected async Task PlaceOrdersAsync()
    {
        if (!CanPlaceOrder)
            return;

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            // Build shared request DTO expected by the server (use Shared.DTOs.Orders types)
            var items = Selected
                .Select(kvp => new OrderRequestItemDto(kvp.Key.MealId, kvp.Key.Date, kvp.Value))
                .ToList();

            var request = new PlaceOrdersRequestDto(items);

            // Call the strongly-typed service that posts the shared DTO
            PlaceOrdersResponse? response = await OrderDataService.PlaceOrdersAsync(request);

            if (response is not null && response.Created > 0)
            {
                SuccessMessage = $"Order placed successfully ({response.Created} items).";
                // clear selection
                Selected.Clear();

                // reload user's persisted items from today forward
                await LoadMyOrdersAsync();
            }
            else
            {
                ErrorMessage = "Failed to place order.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error while placing order: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task<bool> DeleteOrderByIdAsync(Guid orderId)
    {
        try
        {
            bool ok = await OrderDataService.DeleteOrderAsync(orderId);
            if (ok)
                await LoadMyOrdersAsync();
            return ok;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete order: {ex.Message}";
            return false;
        }
    }

    protected async Task<bool> DeleteOrdersForMealAndDateAsync(Guid mealId, DateTime date)
    {
        try
        {
            bool ok = false;//await OrderDataService.DeleteOrdersForMealAndDateAsync(mealId, date);
            if (ok)
                await LoadMyOrdersAsync();
            return ok;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete orders: {ex.Message}";
            return false;
        }
    }
}