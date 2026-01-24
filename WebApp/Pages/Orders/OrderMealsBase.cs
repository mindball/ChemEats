using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace WebApp.Pages.Orders;

public class OrderMealsBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;
    [Inject] protected ISettingsDataService SettingsDataService { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }

    protected Guid? SelectedSupplierId { get; set; }
    protected IReadOnlyList<(Guid Id, string Name)> SuppliersForFilter { get; private set; } = [];

    protected IEnumerable<MenuDto> FilteredMenus => Menus is null
        ? []
        : Menus
            .Where(m => !FilterDate.HasValue || m.Date.Date == FilterDate.Value.Date)
            .Where(m => !SelectedSupplierId.HasValue || m.SupplierId == SelectedSupplierId.Value);

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

    // Server-sourced portion
    private readonly decimal _fallbackPortion = 3.00m;
    protected decimal PortionAmount { get; private set; }

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

            Menus = (await MenuDataService.GetAllMenusAsync(includeDeleted: true)).ToList();

            FilterDate = DateTime.Today.AddDays(1);

            SuppliersForFilter = Menus
                .GroupBy(m => (m.SupplierId, m.SupplierName))
                .Select(g => (g.Key.SupplierId, g.Key.SupplierName))
                .OrderBy(x => x.SupplierName)
                .ToList();

            await LoadPortionAsync();
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

    protected async Task LoadMyOrdersAsync()
    {
        try
        {
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
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName[1..];
        return $"{capitalizedDay} {date:dd.MM.yyyy'ã.'}";
    }

    protected bool IsSelected((Guid MealId, DateTime Date) key) => Selected.ContainsKey(key);
    protected int GetQuantity((Guid MealId, DateTime Date) key) => Selected.GetValueOrDefault(key, 0);

    protected void ToggleSelection((Guid MealId, DateTime Date) key, bool isSelected)
    {
        if (isSelected)
            Selected.TryAdd(key, 1);
        else
            Selected.Remove(key);
    }

    protected void UpdateQuantity((Guid MealId, DateTime Date) key, string value)
    {
        if (!int.TryParse(value, out int q) || q < 0)
            q = 0;

        if (q == 0)
            Selected.Remove(key);
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
            List<OrderRequestItemDto> items = Selected
                .Select(kvp => new OrderRequestItemDto(kvp.Key.MealId, kvp.Key.Date, kvp.Value))
                .ToList();

            PlaceOrdersRequestDto request = new(items);

            PlaceOrdersResponse? response = await OrderDataService.PlaceOrdersAsync(request);

            if (response is not null && response.Created > 0)
            {
                SuccessMessage = $"Order placed successfully ({response.Created} items).";
                Selected.Clear();
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
            bool ok = false; // placeholder if implementing batch delete
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

    // Summary helpers

    protected record SummaryItem(string Name, string SupplierName, DateTime MenuDate, int Quantity, decimal Price);

    protected IReadOnlyList<SummaryItem> BuildSummaryFromSelection()
    {
        if (Menus is null || Selected.Count == 0)
            return [];

        var flatMeals = Menus
            .SelectMany(m => m.Meals.Select(me => new { MenuDate = m.Date, m.SupplierName, Meal = me }))
            .ToList();

        var summary = Selected
            .Select(kvp =>
            {
                var entry = flatMeals.FirstOrDefault(x => x.Meal.Id == kvp.Key.MealId && x.MenuDate == kvp.Key.Date);
                return new SummaryItem(
                    Name: entry?.Meal.Name ?? "Unknown",
                    SupplierName: entry?.SupplierName ?? "Unknown supplier",
                    MenuDate: kvp.Key.Date,
                    Quantity: kvp.Value,
                    Price: entry?.Meal.Price ?? 0m
                );
            })
            .ToList();

        return summary;
    }

    // Apply portion once per calendar date across all suppliers
    protected sealed record PerDateTotal(DateTime Date, IReadOnlyList<SummaryItem> Items, decimal Subtotal, decimal PortionApplied, decimal Total);

    protected IReadOnlyList<PerDateTotal> ComputePerDateTotalsWithPortion(IReadOnlyList<SummaryItem> items, decimal portion)
    {
        var perDate = items
            .GroupBy(x => x.MenuDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
            decimal subtotal = g.Sum(x => x.Price * x.Quantity);

            // Portion applies to a single unit of the first item (if any) for this date
            SummaryItem? firstItem = g.FirstOrDefault(x => x.Quantity > 0);
            decimal portionApplied = 0m;
            if (firstItem is not null)
                portionApplied = Math.Min(portion, firstItem.Price);

            decimal total = subtotal - portionApplied;

            return new PerDateTotal(
                Date: g.Key,
                Items: g.ToList(),
                Subtotal: subtotal,
                PortionApplied: portionApplied,
                Total: total
            );
        })
        .ToList();

        return perDate;
    }
}