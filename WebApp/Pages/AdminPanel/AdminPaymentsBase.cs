using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace WebApp.Pages.AdminPanel;

public class AdminPaymentsBase : ComponentBase
{
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;
    [Inject] protected IEmployeeDataService EmployeeDataService { get; init; } = null!;
    [Inject] protected ISettingsDataService SettingsDataService { get; init; } = null!;

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    protected List<EmployeeDto>? AllEmployees { get; private set; }
    protected List<EmployeeDto> FilteredEmployees => FilterEmployees();
    protected string? SearchTerm { get; set; }
    protected string? SelectedUserId { get; set; }
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected string PaymentStatusFilter { get; set; } = "All";

    protected List<UserOrderPaymentItemDto>? AllOrders { get; private set; }
    protected List<UserOrderPaymentItemDto> FilteredOrders => FilterOrdersByPaymentStatus();
    protected HashSet<Guid> SelectedOrderIds { get; private set; } = [];

    protected bool IsLoading { get; private set; }
    protected bool IsProcessing { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected bool HasSelection => SelectedOrderIds.Count > 0;

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
    protected int UnpaidOrders => FilteredOrders.Count(o => o.PaymentStatus == PaymentStatusDto.Unpaid);
    protected decimal TotalPaid => FilteredOrders.Where(o => o.PaymentStatus == PaymentStatusDto.Paid).Sum(o => o.NetAmount);
    protected decimal TotalUnpaid => FilteredOrders.Where(o => o.PaymentStatus == PaymentStatusDto.Unpaid).Sum(o => o.NetAmount);

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
            EndDate = DateTime.Today;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load employees: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ErrorMessage = "Please select an employee.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        AllOrders = null;
        SelectedOrderIds.Clear();

        try
        {
            AllOrders = await OrderDataService.GetOrdersByUserForPeriodAsync(
                SelectedUserId,
                StartDate,
                EndDate);

            if (AllOrders.Count == 0)
                SuccessMessage = "No orders found for this period.";
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
            "Unpaid" => AllOrders.Where(o => o.PaymentStatus == PaymentStatusDto.Unpaid).ToList(),
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

    protected void SelectAll()
    {
        SelectedOrderIds = FilteredOrders
            .Where(o => o.PaymentStatus == PaymentStatusDto.Unpaid)
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

            SuccessMessage = $"Successfully marked {result.PaidCount} orders as paid. Total: {result.TotalPaid.ToString("C", CultureInfo.CurrentCulture)}";
            SelectedOrderIds.Clear();

            // Refresh the list
            await SearchOrdersAsync();
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
            .Where(o => SelectedOrderIds.Contains(o.OrderId) && o.PaymentStatus == PaymentStatusDto.Unpaid)
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

            UserOrderPaymentItemDto? firstOrder = group.FirstOrDefault();
            if (firstOrder is not null)
            {
                map[firstOrder.OrderId] = Math.Min(PortionAmount, firstOrder.Price);
                datesAlreadyWithPortion.Add(group.Key);
            }
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
}