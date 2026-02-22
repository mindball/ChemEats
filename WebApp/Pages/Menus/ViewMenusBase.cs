    using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using WebApp.Infrastructure.States;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Menus;

public partial class ViewMenusBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected ISupplierDataService SupplierDataService { get; init; } = null!;
    [Inject] protected CustomAuthStateProvider AuthProvider { get; init; } = null!;
    [Inject] protected IMenuReportService MenuReportService { get; set; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }
    protected List<SupplierDto> Suppliers { get; private set; } = [];

    protected Guid? SelectedSupplierId { get; set; }
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected bool IncludeDeleted { get; set; }
    protected bool IsAdmin;

    protected string? ErrorMessage { get; set; }
    protected bool IsLoading { get; private set; }
    protected Guid? ReportLoadingMenuId { get; private set; }

    protected int CurrentPage { get; private set; } = 1;
    protected int PageSize { get; } = 5;
    protected int TotalMenus { get; private set; }
    protected int TotalPages => (int)Math.Ceiling(TotalMenus / (double)PageSize);


    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthProvider.GetAuthenticationStateAsync();
        IsAdmin = authState.User.IsInRole("Admin");

        Suppliers = (await SupplierDataService.GetAllSuppliersAsync()).ToList();

        // Default: today only
        StartDate = DateTime.Today;
        EndDate = DateTime.Today.AddDays(1);
    }

    protected async Task OnSearchClickedAsync()
    {
        CurrentPage = 1;
        await LoadMenusAsync();
    }

    protected async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages)
            return;

        CurrentPage = page;
        await LoadMenusAsync();
    }

    private async Task LoadMenusAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            IEnumerable<MenuDto> allMenus = await MenuDataService.GetAllMenusAsync(IncludeDeleted);

            // Apply filters
            IQueryable<MenuDto> query = allMenus.AsQueryable();

            if (SelectedSupplierId.HasValue)
                query = query.Where(m => m.SupplierId == SelectedSupplierId);

            if (StartDate.HasValue)
                query = query.Where(m => m.Date.Date >= StartDate.Value.Date);

            if (EndDate.HasValue)
                query = query.Where(m => m.Date.Date <= EndDate.Value.Date);

            // Sort by date descending (newest first)
            query = query.OrderByDescending(m => m.Date);

            // Count total results before pagination
            TotalMenus = query.Count();

            // Apply pagination
            Menus = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
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
        CultureInfo bgCulture = new CultureInfo("bg-BG");
        string dayName = date.ToString("dddd", bgCulture);

        // Capitalize first letter
        string capitalizedDay = char.ToUpper(dayName[0], bgCulture) + dayName[1..];

        return $"{capitalizedDay} {date:dd.MM.yyyy'г.'}";
    }

    protected async Task DownloadReportAsync(Guid menuId)
    {
        try
        {
            ReportLoadingMenuId = menuId;
            ErrorMessage = null;

            byte[] pdfBytes = await MenuReportService.GenerateMenuReportAsync(menuId);

            string fileName = $"menu-report-{menuId:N}-{DateTime.Now:yyyyMMdd}.pdf";

            await JsRuntime.InvokeVoidAsync(
                "downloadFile",
                fileName,
                Convert.ToBase64String(pdfBytes),
                "application/pdf");
        }
        catch (ApplicationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error while generating report.";
        }
        finally
        {
            ReportLoadingMenuId = null;
        }
    }

    protected async Task EditDateAsync(Guid id, DateTime current)
    {
        string? newDateStr = await JsRuntime.InvokeAsync<string>("prompt", $"New date (yyyy-MM-dd)", current.ToString("yyyy-MM-dd"));
        if (string.IsNullOrWhiteSpace(newDateStr)) return;
        if (!DateTime.TryParse(newDateStr, out DateTime newDate)) return;

        bool ok = await MenuDataService.UpdateMenuDateAsync(id, newDate);
        if (ok)
            await OnSearchClickedAsync();
        else
            ErrorMessage = "Failed to update menu date.";
    }

    protected async Task SoftDeleteAsync(Guid id)
    {
        if (!await JsRuntime.InvokeAsync<bool>("confirm", "Soft delete this menu and cancel its orders?"))
            return;

        try
        {
            await MenuDataService.SoftDeleteMenuAsync(id);
            await OnSearchClickedAsync();
        }
        catch (ApplicationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error while deleting menu.";
        }
    }

    protected async Task FinalizeMenuAsync(Guid menuId)
    {
        if (!await JsRuntime.InvokeAsync<bool>("confirm", "Finalize this menu? All pending orders will be marked as completed and ordering will be stopped."))
            return;

        try
        {
            FinalizeMenuResponseDto? result = await MenuDataService.FinalizeMenuAsync(menuId);
            if (result is not null)
            {
                await JsRuntime.InvokeVoidAsync("alert",
                    $"Menu finalized successfully!\n{result.CompletedOrdersCount} orders completed.\nTotal amount: {result.TotalAmount:C}");
                await OnSearchClickedAsync();
            }
        }
        catch (ApplicationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error while finalizing menu.";
        }
    }
}
