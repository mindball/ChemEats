using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace WebApp.Pages.AdminPanel;

public class AdminPaymentsBase : ComponentBase
{
    public const string BasePageUrl = "/admin/payments";

    public static string EmployeeAllUnpaidPageUrl(string userId) =>
        $"{BasePageUrl}?userId={Uri.EscapeDataString(userId)}&allDates=true";

    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;
    [Inject] protected IEmployeeDataService EmployeeDataService { get; init; } = null!;
    [Inject] protected ISettingsDataService SettingsDataService { get; init; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "userId")] public string? QueryUserId { get; set; }
    [SupplyParameterFromQuery(Name = "allDates")] public bool? QueryAllDates { get; set; }

    protected List<EmployeeDto>? AllEmployees { get; private set; }
    protected List<EmployeeDto> FilteredEmployees => FilterEmployees();
    protected string? SearchTerm { get; set; }
    protected string? SelectedUserId { get; set; }
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }

    // Ignore the date filter and show every unpaid order of the employee.
    protected bool AllDates { get; set; }
    protected string PaymentStatusFilter { get; set; } = "All";

    protected List<UserOrderPaymentItemDto>? AllOrders { get; private set; }
    protected List<UserOrderPaymentItemDto> FilteredOrders => FilterOrdersByPaymentStatus();
    protected HashSet<Guid> SelectedOrderIds { get; private set; } = [];

    protected bool IsLoading { get; private set; }
    protected bool IsProcessing { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected bool HasSelection => SelectedOrderIds.Count > 0;

    // Outstanding totals for the selected employee up to the loaded end date, including orders before the start date.
    protected EmployeeOutstandingSummaryDto? Outstanding { get; private set; }
    protected DateTime? LoadedStartDate { get; private set; }
    protected DateTime? LoadedEndDate { get; private set; }
    protected bool LoadedAllDates { get; private set; }
    protected bool HasLoadedRange => LoadedAllDates || LoadedEndDate.HasValue;
    protected string PayAllScopeText =>
        LoadedAllDates ? "regardless of date" : $"up to {LoadedEndDate:dd.MM.yyyy}";

    protected int EarlierUnpaidCount => Outstanding?.EarlierUnpaidCount ?? 0;
    protected decimal EarlierNetAmount => Outstanding?.EarlierNetAmount ?? 0m;
    protected int UnpaidUpToEndCount => (Outstanding?.UnpaidCount ?? 0) + EarlierUnpaidCount;
    protected decimal TotalDueUpToEnd => (Outstanding?.NetAmount ?? 0m) + EarlierNetAmount;

    // Only completed orders (finalized menus) can be paid; pending and cancelled ones are shown but not selectable.
    protected static bool IsPayable(UserOrderPaymentItemDto order) =>
        order.PaymentStatus == PaymentStatusDto.Unpaid && order.IsCompleted;

    protected bool IsAllUnpaidSelected
    {
        get
        {
            List<UserOrderPaymentItemDto> unpaid = FilteredOrders
                .Where(IsPayable)
                .ToList();
            return unpaid.Count > 0 && unpaid.All(o => SelectedOrderIds.Contains(o.OrderId));
        }
    }

    protected decimal PortionAmount { get; private set; }

    protected decimal SelectedTotal => FilteredOrders
        .Where(o => SelectedOrderIds.Contains(o.OrderId))
        .Sum(o => o.Price);

    protected decimal EstimatedPortionDeduction => PortionMap.Values.Sum();

    protected decimal EstimatedNetTotal => SelectedTotal - EstimatedPortionDeduction;

    private Dictionary<Guid, decimal>? _portionMapCache;
    private int _portionMapSelectionHash;

    protected Dictionary<Guid, decimal> PortionMap
    {
        get
        {
            int currentHash = ComputeSelectionHash();
            if (_portionMapCache is null || _portionMapSelectionHash != currentHash)
            {
                _portionMapCache = BuildPortionMap();
                _portionMapSelectionHash = currentHash;
            }
            return _portionMapCache;
        }
    }

    protected decimal GetEstimatedPortionForOrder(UserOrderPaymentItemDto item)
    {
        if (item.PaymentStatus == PaymentStatusDto.Paid)
            return item.PortionAmount;

        return PortionMap.GetValueOrDefault(item.OrderId, 0m);
    }

    protected decimal GetEstimatedNetForOrder(UserOrderPaymentItemDto item)
    {
        if (item.PaymentStatus == PaymentStatusDto.Paid)
            return item.NetAmount;

        decimal portion = GetEstimatedPortionForOrder(item);
        return Math.Max(0m, item.Price - portion);
    }

    protected int TotalOrders => FilteredOrders.Count;
    protected int PaidOrders => FilteredOrders.Count(o => o.PaymentStatus == PaymentStatusDto.Paid);
    protected int UnpaidOrders => FilteredOrders.Count(IsPayable);
    protected decimal TotalPaid => FilteredOrders.Where(o => o.PaymentStatus == PaymentStatusDto.Paid).Sum(o => o.NetAmount);
    protected decimal TotalUnpaid => FilteredOrders.Where(IsPayable).Sum(o => o.NetAmount);

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        try
        {
            List<EmployeeDto> employees = await EmployeeDataService.GetAllEmployeesAsync();
            
            AllEmployees = employees.OrderBy(e => e.FullName).ToList();

            PortionAmount = await SettingsDataService.GetCompanyPortionAsync();

            // Default: current month
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load employees: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        // Deep link from the Control Panel: open an employee's unpaid orders directly.
        if (!string.IsNullOrWhiteSpace(QueryUserId))
        {
            SelectedUserId = QueryUserId;
            AllDates = QueryAllDates == true;
            await SearchOrdersAsync();
        }
    }

    private List<EmployeeDto> FilterEmployees()
    {
        if (AllEmployees is null)
            return [];

        if (string.IsNullOrWhiteSpace(SearchTerm))
            return AllEmployees;

        string searchLower = SearchTerm.ToLowerInvariant();

        return AllEmployees
            .Where(e => 
                e.FullName.ToLowerInvariant().Contains(searchLower) ||
                (e.Abbreviation?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (e.Email?.ToLowerInvariant().Contains(searchLower) ?? false))
            .ToList();
    }

    protected async Task SearchOrdersAsync()
    {
        SuccessMessage = null;
        await LoadOrdersAsync();
    }

    // Keeps SuccessMessage so a payment confirmation survives the refresh that follows it.
    private async Task LoadOrdersAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ErrorMessage = "Please select an employee.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        AllOrders = null;
        Outstanding = null;
        LoadedStartDate = null;
        LoadedEndDate = null;
        LoadedAllDates = false;
        SelectedOrderIds.Clear();

        try
        {
            List<EmployeeOutstandingSummaryDto>? outstanding;

            if (AllDates)
            {
                AllOrders = await OrderDataService.GetUnpaidOrdersByUserAsync(SelectedUserId);
                outstanding = await OrderDataService.GetAllOutstandingAsync(SelectedUserId);
                LoadedAllDates = true;
                PaymentStatusFilter = "All";
            }
            else
            {
                AllOrders = await OrderDataService.GetOrdersByUserForPeriodAsync(
                    SelectedUserId,
                    StartDate,
                    EndDate);

                LoadedStartDate = StartDate?.Date;
                LoadedEndDate = (EndDate ?? DateTime.Today).Date;
                DateTime outstandingStart = LoadedStartDate ?? DateTime.MinValue.Date;

                outstanding = await OrderDataService.GetOutstandingSummaryAsync(
                    outstandingStart,
                    LoadedEndDate.Value,
                    SelectedUserId);
            }

            Outstanding = outstanding?.FirstOrDefault(s => s.UserId == SelectedUserId);

            if (AllOrders.Count == 0 && EarlierUnpaidCount == 0)
                SuccessMessage ??= LoadedAllDates ? "No unpaid orders." : "No orders found for this period.";
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

    private List<UserOrderPaymentItemDto> FilterOrdersByPaymentStatus()
    {
        if (AllOrders is null)
            return [];

        return PaymentStatusFilter switch
        {
            "Paid" => AllOrders.Where(o => o.PaymentStatus == PaymentStatusDto.Paid).ToList(),
            "Unpaid" => AllOrders.Where(IsPayable).ToList(),
            _ => AllOrders
        };
    }

    protected void ToggleOrderSelection(Guid orderId, bool isSelected)
    {
        if (isSelected)
            SelectedOrderIds.Add(orderId);
        else
            SelectedOrderIds.Remove(orderId);

        _portionMapCache = null;
    }

    protected void ToggleSelectAll(bool selectAll)
    {
        if (selectAll)
            SelectAll();
        else
            DeselectAll();
    }

    protected void SelectAll()
    {
        SelectedOrderIds = FilteredOrders
            .Where(IsPayable)
            .Select(o => o.OrderId)
            .ToHashSet();

        _portionMapCache = null;
    }

    protected void DeselectAll()
    {
        SelectedOrderIds.Clear();
        _portionMapCache = null;
    }

    protected async Task MarkSelectedAsPaidAsync()
    {
        if (!HasSelection || string.IsNullOrWhiteSpace(SelectedUserId))
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            OrderPayRequestDto requestDto = new(SelectedUserId, SelectedOrderIds.ToList());
            OrderPayResponseDto? result = await OrderDataService.OrderMarkAsPaidAsync(requestDto);

            if (result is null)
            {
                ErrorMessage = "Failed to process payment. Please try again.";
                return;
            }

            SuccessMessage = $"Successfully marked {result.PaidCount} orders as paid. Total: {FormatEuro(result.TotalPaid)}";
            SelectedOrderIds.Clear();

            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Payment processing failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task PayAllUnpaidAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedUserId) || !HasLoadedRange || UnpaidUpToEndCount == 0)
            return;

        string employeeName = AllEmployees?.FirstOrDefault(e => e.UserId == SelectedUserId)?.FullName ?? SelectedUserId;
        bool confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Mark all {UnpaidUpToEndCount} unpaid orders of {employeeName} {PayAllScopeText} as paid?\nTotal: {FormatEuro(TotalDueUpToEnd)}");

        if (!confirmed)
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            OrderPayResponseDto? result = await OrderDataService.PayAllUnpaidAsync(
                new PayAllUnpaidRequestDto(SelectedUserId, LoadedAllDates ? null : LoadedEndDate));

            if (result is null)
            {
                ErrorMessage = "Failed to process payment. Please try again.";
                return;
            }

            SuccessMessage = $"Successfully marked {result.PaidCount} orders as paid. Total: {FormatEuro(result.TotalPaid)}";
            SelectedOrderIds.Clear();

            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Payment processing failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private Dictionary<Guid, decimal> BuildPortionMap()
    {
        Dictionary<Guid, decimal> map = new();

        if (PortionAmount <= 0m || !HasSelection)
            return map;

        List<UserOrderPaymentItemDto> selectedUnpaid = FilteredOrders
            .Where(o => SelectedOrderIds.Contains(o.OrderId) && IsPayable(o))
            .ToList();

        HashSet<DateOnly> datesAlreadyWithPortion = FilteredOrders
            .Where(o => o.PortionApplied && !SelectedOrderIds.Contains(o.OrderId))
            .Select(o => DateOnly.FromDateTime(o.MenuDate))
            .ToHashSet();

        foreach (IGrouping<DateOnly, UserOrderPaymentItemDto> group in selectedUnpaid
            .GroupBy(o => DateOnly.FromDateTime(o.MenuDate)))
        {
            if (datesAlreadyWithPortion.Contains(group.Key))
                continue;

            // Same rule as the server: the portion goes to the most expensive order of the day.
            UserOrderPaymentItemDto mostExpensive = group
                .OrderByDescending(o => o.Price)
                .ThenBy(o => o.OrderedAt)
                .First();

            map[mostExpensive.OrderId] = Math.Min(PortionAmount, mostExpensive.Price);
            datesAlreadyWithPortion.Add(group.Key);
        }

        return map;
    }

    private int ComputeSelectionHash()
    {
        HashCode hash = new();
        foreach (Guid id in SelectedOrderIds.Order())
            hash.Add(id);
        return hash.ToHashCode();
    }

    protected static string FormatBulgarianDate(DateTime date)
    {
        CultureInfo bgCulture = new("bg-BG");
        string dayName = date.ToString("dddd", bgCulture);
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName[1..];
        return $"{capitalizedDay} {date:dd.MM.yyyy'г.'}";
    }

    protected static string FormatEuro(decimal amount)
    {
        NumberFormatInfo numberFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();

        if (ShouldForceEuroForBulgarianLev())
            numberFormat.CurrencySymbol = "€";

        return amount.ToString("C", numberFormat);
    }

    private static bool ShouldForceEuroForBulgarianLev()
    {
        string currencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
        bool usesLevSymbol = currencySymbol.Contains("лв", StringComparison.OrdinalIgnoreCase);
        bool usesLevIsoCode = string.Equals(
            RegionInfo.CurrentRegion.ISOCurrencySymbol,
            "BGN",
            StringComparison.OrdinalIgnoreCase);

        return usesLevSymbol || usesLevIsoCode;
    }
}