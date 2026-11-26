using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Reports;
using WebApp.Services.Menus;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Reports;

public class MenuReportBase : LocalizedComponentBase
{
    public const string MenusPageUrl = "/menus/view";

    public static string PageUrl(Guid menuId) => $"/reports/menu/{menuId}";

    [Inject] protected IMenuReportService MenuReportService { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

    [Parameter] public Guid MenuId { get; set; }

    protected MenuReportDto? Report { get; private set; }
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }
    protected DateTime GeneratedAt { get; private set; }

    protected decimal TotalAmount { get; private set; }
    protected IReadOnlyList<(string MealName, int Count)> MealSummaries { get; private set; } = [];

    // Browsers use the document title as the default file name in "Save as PDF".
    protected string DocumentTitle =>
        Report is null
            ? L["Menu Report"]
            : $"menu-report-{Report.SupplierName}-{Report.MenuDate:yyyyMMdd}";

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Report = null;
        TotalAmount = 0m;
        MealSummaries = [];

        try
        {
            Report = await MenuReportService.GetMenuReportAsync(MenuId);
            GeneratedAt = DateTime.Now;

            if (Report is null)
            {
                ErrorMessage = L["The report could not be loaded. The menu may not exist or you may not have access to it."];
                return;
            }

            TotalAmount = Report.Orders.Sum(order => order.Price);
            MealSummaries = Report.Orders
                .GroupBy(order => order.MealName)
                .Select(group => (MealName: group.Key, Count: group.Count()))
                .OrderByDescending(summary => summary.Count)
                .ThenBy(summary => summary.MealName)
                .ToList();
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
        NavigationManager.NavigateTo(MenusPageUrl);
    }
}
