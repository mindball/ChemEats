using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Menus;

public class UploadMenuBase : ComponentBase
{
    [Inject] protected IMenuDataService MenuService { get; set; } = null!;
    [Inject] protected ISupplierDataService SupplierService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected List<SupplierDto> Suppliers { get; set; } = [];
    protected List<CreateMealDto> ParsedMeals { get; set; } = [];

    protected Guid SelectedSupplierId { get; set; }
    private DateTime _selectedDate = DateTime.Today.AddDays(1);
    protected DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (value.Date <= DateTime.Today)
            {
                ErrorMessage = "Menu date must be in the future.";
                _selectedDate = DateTime.Today.AddDays(1);
            }
            else
            {
                ErrorMessage = null;
                _selectedDate = value;
            }
            StateHasChanged();
        }
    }

    protected bool IsLoading { get; set; }
    protected bool IsSubmitting { get; set; }
    protected bool IsParsingFile { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected bool IsRedirecting { get; set; }
    protected int RedirectCountdown { get; set; }

    private string _activeUntilTimeString = "12:00";

    protected string ActiveUntilTimeString
    {
        get => _activeUntilTimeString;
        set
        {
            _activeUntilTimeString = value;
            StateHasChanged();
        }
    }

    protected TimeOnly ActiveUntilTime
    {
        get
        {
            string[] formats = ["HH:mm", "HH:mm:ss", "H:mm", "H:mm:ss"];

            if (TimeOnly.TryParseExact(
                _activeUntilTimeString,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out TimeOnly result))
            {
                return result;
            }

            if (TimeOnly.TryParse(_activeUntilTimeString, CultureInfo.InvariantCulture, out TimeOnly fallbackResult))
            {
                return fallbackResult;
            }

            return new TimeOnly(12, 0); 
        }
    }

    protected DateTime ActiveUntil => DateTime.Today.Add(ActiveUntilTime.ToTimeSpan());

    protected override async Task OnInitializedAsync()
    {
        await LoadSuppliersAsync();
    }

    protected async Task LoadSuppliersAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            IEnumerable<SupplierDto> suppliers = await SupplierService.GetAllSuppliersAsync();
            Suppliers = suppliers.ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load suppliers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        IBrowserFile file = e.File;

        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            ParsedMeals.Clear();
            IsParsingFile = true;
            StateHasChanged();

            const long maxFileSize = 5 * 1024 * 1024;

            await using Stream stream = file.OpenReadStream(maxFileSize);
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            List<CreateMealDto> meals = await MenuService.ParseMenuFileAsync(memoryStream, file.Name);

            if (meals.Count == 0)
            {
                ErrorMessage = "No meals found in the uploaded file.";
                return;
            }

            ParsedMeals = meals;
            SuccessMessage = $"Successfully parsed {ParsedMeals.Count} meal(s) from file.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to parse file: {ex.Message}";
            ParsedMeals.Clear();
        }
        finally
        {
            IsParsingFile = false;
        }
    }

    protected void ResetForm()
    {
        SelectedSupplierId = Guid.Empty;
        SelectedDate = DateTime.Today.AddDays(1);
        _activeUntilTimeString = "12:00";
        ParsedMeals.Clear();
        ErrorMessage = null;
        SuccessMessage = null;
        IsRedirecting = false;
        RedirectCountdown = 0;
    }

    protected async Task HandleUploadAsync()
    {
        try
        {
            IsSubmitting = true;
            ErrorMessage = null;
            SuccessMessage = null;
            IsRedirecting = false;

            if (SelectedSupplierId == Guid.Empty)
            {
                ErrorMessage = "Please select a supplier.";
                return;
            }

            if (!ParsedMeals.Any())
            {
                ErrorMessage = "Please upload a file with meals.";
                return;
            }

            if (ParsedMeals.Any(m => string.IsNullOrWhiteSpace(m.Name)))
            {
                ErrorMessage = "All meals must have a name.";
                return;
            }

            if (ParsedMeals.Any(m => m.Price <= 0))
            {
                ErrorMessage = "All meals must have a price greater than 0.";
                return;
            }

            TimeOnly minTime = new(8, 0);
            TimeOnly maxTime = new(16, 0);

            if (ActiveUntilTime < minTime || ActiveUntilTime > maxTime)
            {
                ErrorMessage = $"Active Until time must be between {minTime:HH:mm} and {maxTime:HH:mm}.";
                return;
            }

            if (ActiveUntil <= DateTime.Now)
            {
                ErrorMessage = "Active Until time must be in the future.";
                return;
            }

            CreateMenuDto dto = new(
                SelectedSupplierId,
                SelectedDate,
                ActiveUntil,
                ParsedMeals
            );

            MenuDto? result = await MenuService.AddMenuAsync(dto);

            if (result is not null)
            {
                SuccessMessage = $"Menu uploaded successfully! {ParsedMeals.Count} meal(s) added. Active until {result.ActiveUntil:dd.MM.yyyy HH:mm}";
                ResetForm();

                IsRedirecting = true;
                RedirectCountdown = 3;

                for (int i = RedirectCountdown; i > 0; i--)
                {
                    RedirectCountdown = i;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(1000);
                }

                Navigation.NavigateTo("/menus/order");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}