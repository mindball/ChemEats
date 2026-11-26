using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Orders;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Orders;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Reports;

public class OutstandingReportBase : LocalizedComponentBase
{
    public const string AllDatesPageUrl = "/reports/outstanding?allDates=true";

    public static string PageUrl(DateTime startDate, DateTime endDate) =>
        $"/reports/outstanding?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

    [Inject] protected IOrderDataService OrderDataService { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "startDate")] public DateTime? StartDate { get; set; }
    [SupplyParameterFromQuery(Name = "endDate")] public DateTime? EndDate { get; set; }
    [SupplyParameterFromQuery(Name = "allDates")] public bool? AllDates { get; set; }

    protected bool IsAllDates => AllDates == true;

    protected List<EmployeeOutstandingSummaryDto>? Summaries { get; private set; }
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }
    protected DateTime GeneratedAt { get; private set; }

    protected int TotalUnpaidCount => Summaries?.Sum(s => s.UnpaidCount) ?? 0;
    protected decimal TotalGrossAmount => Summaries?.Sum(s => s.GrossAmount) ?? 0m;
    protected decimal TotalPortionAmount => Summaries?.Sum(s => s.PortionAmount) ?? 0m;
    protected decimal TotalNetAmount => Summaries?.Sum(s => s.NetAmount) ?? 0m;
    protected decimal TotalEarlierNetAmount => Summaries?.Sum(s => s.EarlierNetAmount) ?? 0m;
    protected decimal TotalOtherAmount => Summaries?.Sum(s => s.OtherAmount) ?? 0m;
    protected decimal GrandTotalDue => TotalNetAmount + TotalEarlierNetAmount + TotalOtherAmount;

    protected static decimal TotalDue(EmployeeOutstandingSummaryDto summary) =>
        summary.NetAmount + summary.EarlierNetAmount + summary.OtherAmount;

    protected string PeriodText =>
        IsAllDates
            ? L["All dates"]
            : StartDate.HasValue && EndDate.HasValue
                ? $"{StartDate.Value:dd.MM.yyyy} – {EndDate.Value:dd.MM.yyyy}"
                : string.Empty;

    // Browsers use the document title as the default file name in "Save as PDF".
    protected string DocumentTitle =>
        IsAllDates
            ? $"outstanding-payments-all-{DateTime.Today:yyyyMMdd}"
            : StartDate.HasValue && EndDate.HasValue
                ? $"outstanding-payments-{StartDate.Value:yyyyMMdd}-{EndDate.Value:yyyyMMdd}"
                : L["Outstanding Payments"];

    protected override async Task OnParametersSetAsync()
    {
        ErrorMessage = null;
        Summaries = null;

        if (!IsAllDates)
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                ErrorMessage = L["The report period is missing. Open the report from the Payments summary page."];
                return;
            }

            if (EndDate.Value.Date < StartDate.Value.Date)
            {
                ErrorMessage = L["End date cannot be before start date."];
                return;
            }
        }

        IsLoading = true;

        try
        {
            Summaries = IsAllDates
                ? await OrderDataService.GetAllOutstandingAsync()
                : await OrderDataService.GetOutstandingSummaryAsync(StartDate!.Value, EndDate!.Value);
            GeneratedAt = DateTime.Now;

            if (Summaries is null)
                ErrorMessage = L["The report could not be loaded. Please try again."];
        }
        catch (Exception)
        {
            ErrorMessage = L["Unexpected error while loading the report."];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task PrintAsync()
    {
        await JsRuntime.InvokeVoidAsync("print");
    }

    protected void GoBack()
    {
        NavigationManager.NavigateTo(IsAllDates
            ? AdminPaymentsSummaryBase.AllDatesPageUrl
            : StartDate.HasValue && EndDate.HasValue
                ? AdminPaymentsSummaryBase.PageUrl(StartDate.Value, EndDate.Value)
                : AdminPaymentsSummaryBase.BasePageUrl);
    }
}
