using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace WebApp.Pages.Orders;

public class OrderMenuBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }

    protected IEnumerable<MenuDto> FilteredMenus => Menus is null
        ? []
        : Menus.Where(m => !FilterDate.HasValue || m.Date.Date == FilterDate.Value.Date);

    protected DateTime? FilterDate { get; set; }

    protected string? ErrorMessage { get; private set; }
    protected string? SuccessMessage { get; private set; }
    protected bool IsLoading { get; private set; }

    protected bool IsAuthenticated { get; private set; }
    protected string? CurrentUserName { get; private set; }

    // key: (mealId, menuDate) -> quantity
    protected readonly Dictionary<(Guid MealId, DateTime Date), int> Selected = new();

    protected int SelectedItemsCount => Selected.Values.Sum();

    protected bool CanPlaceOrder => IsAuthenticated && SelectedItemsCount > 0;

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

            Menus = (await MenuDataService.GetAllMenusAsync()).ToList();

            // Default filter date to today if there are menus that match today
            FilterDate = DateTime.Today;
            if (Menus!.All(m => m.Date.Date != FilterDate.Value.Date))
            {
                // if none for today, clear filter
                FilterDate = null;
            }
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

    protected int GetQuantity((Guid MealId, DateTime Date) key)
    {
        return Selected.GetValueOrDefault(key, 1);
    }

    protected void ToggleSelection((Guid MealId, DateTime Date) key, bool isSelected)
    {
        if (isSelected)
        {
            Selected.TryAdd(key, 1);
        }
        else
        {
            Selected.Remove(key);
        }
    }

    protected void UpdateQuantity((Guid MealId, DateTime Date) key, string value)
    {
        if (!int.TryParse(value, out int q) || q < 1)
            q = 1;

        if (!Selected.TryAdd(key, q))
            Selected[key] = q;
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
                Selected.Clear();
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
}