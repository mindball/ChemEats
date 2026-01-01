using System.Globalization;
using Microsoft.AspNetCore.Components;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Menus;

public class ViewMenusBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuDataService { get; init; } = null!;
    [Inject] protected ISupplierDataService SupplierDataService { get; init; } = null!;

    protected IReadOnlyList<MenuDto>? Menus { get; private set; }
    protected List<SupplierDto> Suppliers { get; private set; } = [];

    protected Guid? SelectedSupplierId { get; set; }
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected bool IncludeDeleted { get; set; }

    protected string? ErrorMessage { get; set; }
    protected bool IsLoading { get; private set; }

    protected int CurrentPage { get; private set; } = 1;
    protected int PageSize { get; } = 5;
    protected int TotalMenus { get; private set; }
    protected int TotalPages => (int)Math.Ceiling(TotalMenus / (double)PageSize);


    protected override async Task OnInitializedAsync()
    {
        Suppliers = (await SupplierDataService.GetAllSuppliersAsync()).ToList();

        // Default: today only
        StartDate = DateTime.Today;
        EndDate = DateTime.Today;
    }

    protected async Task OnSearchClickedAsync()
    {
        CurrentPage = 1; // reset to first page when new filters applied
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
}
