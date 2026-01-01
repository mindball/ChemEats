using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using System.Globalization;
using WebApp.Components;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Menus;

public class UploadMenuBase : ComponentBase
{
    [Inject] private ISupplierDataService _supplierDataService { get; init; }
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IMenuDataService _menuDataService { get; init; }
    protected IReadOnlyList<CreateMealDto>? Meals { get; private set; }

    protected List<SupplierDto> Suppliers { get; set; } = [];
    protected Guid? SelectedSupplierId { get; set; }
    protected bool ShowSecondMenu { get; set; }
    protected IReadOnlyList<CreateMealDto>? SecondMeals { get; set; }
    protected Guid? SecondSelectedSupplierId { get; set; }
    protected DateTime? MenuDate { get; set; }

    protected string? ErrorMessage { get; private set; }
    protected string? SuccessMessage { get; private set; }
    protected string? MenuDateError { get; private set; }
    protected bool MenuDateIsValid => MenuDate is not null && MenuDate.Value.Date >= TomorrowDate;
    protected string MenuDateCss => MenuDateIsValid ? "form-control" : "form-control is-invalid border border-red-500";
    protected DateTime TomorrowDate => DateTime.Today.AddDays(1);

    protected override async Task OnInitializedAsync()
    {
        Suppliers = (await _supplierDataService.GetAllSuppliersAsync()).ToList();

        // Default to tomorrow to guide users and meet the requirement
        MenuDate ??= TomorrowDate;

        await base.OnInitializedAsync();
    }

    protected async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        IBrowserFile file = e.File;

        try
        {
            await using Stream readStream = file.OpenReadStream(10 * 1024 * 1024);
            using MemoryStream ms = new();
            await readStream.CopyToAsync(ms);
            ms.Position = 0;

            string? firstLine;
            ms.Position = 0;
            using (StreamReader sr = new(ms, leaveOpen: true))
            {
                firstLine = await sr.ReadLineAsync();
            }

            string delimiter = firstLine != null && firstLine.Contains(";") ? ";" : ",";

            ms.Position = 0;
            using StreamReader reader = new(ms, leaveOpen: true);
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true
            };

            using CsvReader csv = new(reader, config);
            Meals = csv.GetRecords<CreateMealDto>()
                .Select(m => m)
                .ToList()
                .AsReadOnly();
            ;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to read CSV file: {ex.Message}";
            Meals = null;
        }
    }

    protected async Task HandleSecondFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        IBrowserFile file = e.File;

        try
        {
            await using Stream readStream = file.OpenReadStream(10 * 1024 * 1024);
            using MemoryStream ms = new();
            await readStream.CopyToAsync(ms);
            ms.Position = 0;

            string? firstLine;
            ms.Position = 0;
            using (StreamReader sr = new(ms, leaveOpen: true))
            {
                firstLine = await sr.ReadLineAsync();
            }

            string delimiter = firstLine != null && firstLine.Contains(';') ? ";" : ",";

            ms.Position = 0;
            using StreamReader reader = new(ms, leaveOpen: true);
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true
            };

            using CsvReader csv = new(reader, config);
            SecondMeals = csv.GetRecords<CreateMealDto>().ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to read secondary CSV file: {ex.Message}";
            SecondMeals = null;
        }
    }

    // Runs on date change to set error state
    protected Task ValidateMenuDate()
    {
        string tomorrow = TomorrowDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        MenuDateError = MenuDateIsValid
            ? null
            : $"Menu date must be tomorrow or greater than tomorrow ({tomorrow}).";
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected async Task SaveMenusAsync()
    {
        if (!SelectedSupplierId.HasValue)
        {
            await ShowPopupAsync("Error", "Please choose a supplier before saving.", "error");
            return;
        }

        if (Meals is null || !Meals.Any())
        {
            await ShowPopupAsync("Error", "Please add at least one meal before saving.", "warning");
            return;
        }

        if (MenuDate is null)
        {
            await ShowPopupAsync("Error", "Please select a menu date.", "error");
            return;
        }

        if (!MenuDateIsValid)
        {
            var tomorrow = TomorrowDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            await ShowPopupAsync("Error", $"Menu date must be tomorrow ({tomorrow}).", "error");
            return;
        }

        try
        {
            CreateMenuDto menu = new(SelectedSupplierId.Value, MenuDate.Value, Meals.ToList());
            await _menuDataService.AddMenuAsync(menu);
            const string message = "Menu saved successfully!";
            await ShowPopupAsync("Success!", message, "success");
            SuccessMessage = message;

            // Reset UI state to prevent repeated saves and let edits happen in ViewMenu
            Meals = null;
            SecondMeals = null;
            ShowSecondMenu = false;
            SecondSelectedSupplierId = null;
            SelectedSupplierId = null;
            MenuDate = TomorrowDate;
            MenuDateError = null;
            ErrorMessage = null;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await ShowPopupAsync("Error", $"Failed to save menu: {ex.Message}", "error");
        }
    }

    protected Toast? ToastInstance;

    protected void ToastInitialized(Toast toast)
    {
        ToastInstance = toast;
    }

    protected async Task ShowPopupAsync(string title, string message, string icon)
    {
        if (ToastInstance is not null)
        {
            await ToastInstance.ShowAsync(title, message, icon);
        }
    }

}