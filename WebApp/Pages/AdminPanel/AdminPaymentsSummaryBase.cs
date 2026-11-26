using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Orders;
using WebApp.Pages.Reports;
using WebApp.Services.Orders;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.AdminPanel;

public class AdminPaymentsSummaryBase : LocalizedComponentBase
{
    public const string BasePageUrl = "/admin/payments/summary";

    public const string AllDatesPageUrl = BasePageUrl + "?allDates=true";

    public static string PageUrl(DateTime startDate, DateTime endDate) =>
        $"{BasePageUrl}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

    [Inject] protected IOrderDataService OrderDataService { get; init; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; init; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; init; } = null!;

    [SupplyParameterFromQuery(Name = "startDate")] public DateTime? QueryStartDate { get; set; }
    [SupplyParameterFromQuery(Name = "endDate")] public DateTime? QueryEndDate { get; set; }
    [SupplyParameterFromQuery(Name = "allDates")] public bool? QueryAllDates { get; set; }

    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }

    // Ignore the date filter and list every unpaid order.
    protected bool AllDates { get; set; }

    // The period the current results belong to; the date pickers may have been changed since.
    protected DateTime? LoadedStartDate { get; private set; }
    protected DateTime? LoadedEndDate { get; private set; }
    protected bool LoadedAllDates { get; private set; }

    protected string LoadedPeriodText =>
        LoadedAllDates
            ? L["All dates"]
            : LoadedStartDate.HasValue && LoadedEndDate.HasValue
                ? $"{LoadedStartDate.Value:dd.MM.yyyy} – {LoadedEndDate.Value:dd.MM.yyyy}"
                : string.Empty;

    protected List<EmployeeOutstandingSummaryDto>? Summaries { get; private set; }

    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    // The employee whose payment is in progress; all Pay buttons are disabled meanwhile.
    protected string? PayingUserId { get; private set; }

    protected int TotalUnpaidCount => Summaries?.Sum(s => s.UnpaidCount) ?? 0;
    protected decimal TotalGrossAmount => Summaries?.Sum(s => s.GrossAmount) ?? 0m;
    protected decimal TotalPortionAmount => Summaries?.Sum(s => s.PortionAmount) ?? 0m;
    protected decimal TotalNetAmount => Summaries?.Sum(s => s.NetAmount) ?? 0m;
    protected decimal TotalEarlierNetAmount => Summaries?.Sum(s => s.EarlierNetAmount) ?? 0m;
    protected int TotalOtherCount => Summaries?.Sum(s => s.OtherUnpaidCount) ?? 0;
    protected decimal TotalOtherAmount => Summaries?.Sum(s => s.OtherAmount) ?? 0m;
    protected decimal GrandTotalDue => TotalNetAmount + TotalEarlierNetAmount + TotalOtherAmount;

    // Meals and other obligations are paid separately; "Pay" settles only the meals.
    protected static decimal MealsDue(EmployeeOutstandingSummaryDto summary) =>
        summary.NetAmount + summary.EarlierNetAmount;

    protected static bool HasUnpaidMeals(EmployeeOutstandingSummaryDto summary) =>
        summary.UnpaidCount + summary.EarlierUnpaidCount > 0;

    protected static decimal TotalDue(EmployeeOutstandingSummaryDto summary) =>
        MealsDue(summary) + summary.OtherAmount;

    // The summary counts other obligations up to the period end with no lower bound, so the link does the same.
    protected string OtherObligationsUrl(EmployeeOutstandingSummaryDto summary) =>
        AdminObligationsBase.EmployeeUnpaidPageUrl(summary.UserId, LoadedAllDates ? null : LoadedEndDate);

    protected override async Task OnInitializedAsync()
    {
        // Default: current month
        DateTime monthStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        StartDate = QueryStartDate ?? monthStart;
        EndDate = QueryEndDate ?? monthStart.AddMonths(1).AddDays(-1);
        AllDates = QueryAllDates == true;

        await LoadSummaryAsync();
    }

    protected async Task LoadSummaryAsync()
    {
        if (!AllDates)
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                ErrorMessage = L["Please select a start and end date."];
                return;
            }

            if (EndDate.Value.Date < StartDate.Value.Date)
            {
                ErrorMessage = L["End date cannot be before start date."];
                return;
            }
        }

        IsLoading = true;
        ErrorMessage = null;
        Summaries = null;
        LoadedStartDate = null;
        LoadedEndDate = null;
        LoadedAllDates = false;

        try
        {
            List<EmployeeOutstandingSummaryDto>? summaries = AllDates
                ? await OrderDataService.GetAllOutstandingAsync()
                : await OrderDataService.GetOutstandingSummaryAsync(StartDate!.Value, EndDate!.Value);

            if (summaries is null)
            {
                ErrorMessage = L["Failed to load the outstanding summary. Please try again."];
                return;
            }

            Summaries = summaries;

            if (AllDates)
            {
                LoadedAllDates = true;
            }
            else
            {
                LoadedStartDate = StartDate!.Value.Date;
                LoadedEndDate = EndDate!.Value.Date;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to load the outstanding summary: {0}", ex.Message];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task PayEmployeeAsync(EmployeeOutstandingSummaryDto summary)
    {
        if ((!LoadedAllDates && !LoadedEndDate.HasValue) || PayingUserId is not null || !HasUnpaidMeals(summary))
            return;

        int orderCount = summary.UnpaidCount + summary.EarlierUnpaidCount;
        string scope = LoadedAllDates ? L["regardless of date"] : L["up to {0:dd.MM.yyyy}", LoadedEndDate.GetValueOrDefault()];
        bool confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            L["Mark all {0} unpaid orders of {1} {2} as paid?\nTotal: {3}", orderCount, summary.EmployeeName, scope, FormatEuro(MealsDue(summary))].Value);

        if (!confirmed)
            return;

        PayingUserId = summary.UserId;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            OrderPayResponseDto? result = await OrderDataService.PayAllUnpaidAsync(
                new PayAllUnpaidRequestDto(summary.UserId, LoadedAllDates ? null : LoadedEndDate));

            if (result is null)
            {
                ErrorMessage = L["Failed to process the payment for {0}. Please try again.", summary.EmployeeName];
                return;
            }

            SuccessMessage = L["{0}: {1} orders marked as paid. Total: {2}", summary.EmployeeName, result.PaidCount, FormatEuro(result.TotalPaid)];

            // Reload what was paid, even if the filters were changed in the meantime.
            AllDates = LoadedAllDates;
            if (!LoadedAllDates)
            {
                StartDate = LoadedStartDate;
                EndDate = LoadedEndDate;
            }

            await LoadSummaryAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Payment processing failed: {0}", ex.Message];
        }
        finally
        {
            PayingUserId = null;
        }
    }

    protected void OpenPrintReport()
    {
        if (LoadedAllDates)
        {
            NavigationManager.NavigateTo(OutstandingReportBase.AllDatesPageUrl);
            return;
        }

        if (!LoadedStartDate.HasValue || !LoadedEndDate.HasValue)
            return;

        NavigationManager.NavigateTo(OutstandingReportBase.PageUrl(LoadedStartDate.Value, LoadedEndDate.Value));
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
