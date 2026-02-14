using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Services.Employees;
using WebApp.Services.Orders;

namespace WebApp.Pages.AdminPanel;

public class AdminPaymentsBase : ComponentBase
{
    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;
    [Inject] protected IEmployeeDataService EmployeeDataService { get; init; } = null!;

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

    protected decimal SelectedTotal => FilteredOrders
        .Where(o => SelectedOrderIds.Contains(o.OrderId))
        .Sum(o => o.NetAmount);

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
            
            // Подреждане по азбучен ред (FullName)
            AllEmployees = employees.OrderBy(e => e.FullName).ToList();

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
    }

    protected void SelectAll()
    {
        if (FilteredOrders is null) return;

        SelectedOrderIds = FilteredOrders
            .Where(o => o.PaymentStatus == PaymentStatusDto.Unpaid)
            .Select(o => o.OrderId)
            .ToHashSet();
    }

    protected void DeselectAll()
    {
        SelectedOrderIds.Clear();
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

    protected static string FormatBulgarianDate(DateTime date)
    {
        CultureInfo bgCulture = new("bg-BG");
        string dayName = date.ToString("dddd", bgCulture);
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName[1..];
        return $"{capitalizedDay} {date:dd.MM.yyyy'г.'}";
    }
}