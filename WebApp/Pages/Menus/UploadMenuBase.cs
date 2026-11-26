using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Menus;

public class UploadMenuBase : LocalizedComponentBase
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
                ErrorMessage = L["Menu date must be in the future."];
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
            ErrorMessage = L["Failed to load suppliers: {0}", ex.Message];
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
                ErrorMessage = L["No meals found in the uploaded file."];
                return;
            }

            ParsedMeals = meals;
            SuccessMessage = L["Successfully parsed {0} meal(s) from file.", ParsedMeals.Count];
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to parse file: {0}", ex.Message];
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
                ErrorMessage = L["Please select a supplier."];
                return;
            }

            if (!ParsedMeals.Any())
            {
                ErrorMessage = L["Please upload a file with meals."];
                return;
            }

            if (ParsedMeals.Any(m => string.IsNullOrWhiteSpace(m.Name)))
            {
                ErrorMessage = L["All meals must have a name."];
                return;
            }

            if (ParsedMeals.Any(m => m.Price <= 0))
            {
                ErrorMessage = L["All meals must have a price greater than 0."];
                return;
            }

            TimeOnly minTime = new(8, 0);
            TimeOnly maxTime = new(16, 0);

            if (ActiveUntilTime < minTime || ActiveUntilTime > maxTime)
            {
                ErrorMessage = L["Active Until time must be between {0:HH:mm} and {1:HH:mm}.", minTime, maxTime];
                return;
            }

            if (ActiveUntil <= DateTime.Now)
            {
                ErrorMessage = L["Active Until time must be in the future."];
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
                SuccessMessage = L["Menu uploaded successfully! {0} meal(s) added. Active until {1:dd.MM.yyyy HH:mm}", ParsedMeals.Count, result.ActiveUntil];
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