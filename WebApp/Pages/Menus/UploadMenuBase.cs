using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
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
    protected List<CsvMealRow> ParsedMeals { get; set; } = [];
    
    protected Guid SelectedSupplierId { get; set; }
    protected DateTime SelectedDate { get; set; } = DateTime.Today.AddDays(1);
    protected DateTime SelectedActiveUntil { get; set; } = DateTime.Today.AddDays(1).AddHours(12);
    
    protected bool IsLoading { get; set; }
    protected bool IsSubmitting { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected TimeOnly ActiveUntilTime
    {
        get => TimeOnly.FromDateTime(SelectedActiveUntil);
        set => SelectedActiveUntil = SelectedDate.Date.Add(value.ToTimeSpan());
    }

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
        IBrowserFile? file = e.File;

        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            ParsedMeals.Clear();

            const long maxFileSize = 5 * 1024 * 1024;
            
            await using Stream stream = file.OpenReadStream(maxFileSize);
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            using StreamReader reader = new(memoryStream, Encoding.UTF8);
            
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using CsvReader csv = new(reader, config);
            csv.Context.RegisterClassMap<CsvMealRowMap>();
            
            List<CsvMealRow> records = csv.GetRecords<CsvMealRow>().ToList();
            
            if (!records.Any())
            {
                ErrorMessage = "CSV file is empty or invalid format.";
                return;
            }

            ParsedMeals = records;
            SuccessMessage = $"Successfully loaded {ParsedMeals.Count} meal(s) from CSV.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to parse CSV file: {ex.Message}";
            ParsedMeals.Clear();
        }
    }

    protected void ResetForm()
    {
        SelectedSupplierId = Guid.Empty;
        SelectedDate = DateTime.Today.AddDays(1);
        SelectedActiveUntil = DateTime.Today.AddDays(1).AddHours(12);
        ParsedMeals.Clear();
        ErrorMessage = null;
        SuccessMessage = null;
    }

    protected async Task HandleUploadAsync()
    {
        try
        {
            IsSubmitting = true;
            ErrorMessage = null;
            SuccessMessage = null;

            if (SelectedSupplierId == Guid.Empty)
            {
                ErrorMessage = "Please select a supplier.";
                return;
            }

            if (!ParsedMeals.Any())
            {
                ErrorMessage = "Please upload a CSV file with meals.";
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

            DateTime activeUntil = SelectedDate.Date.Add(SelectedActiveUntil.TimeOfDay);

            List<CreateMealDto> mealDtos = ParsedMeals
                .Select(m => new CreateMealDto(m.Name, m.Price))
                .ToList();

            CreateMenuDto dto = new(
                SelectedSupplierId,
                SelectedDate,
                activeUntil,
                mealDtos
            );

            MenuDto? result = await MenuService.AddMenuAsync(dto);

            if (result is not null)
            {
                SuccessMessage = $"Menu uploaded successfully! {ParsedMeals.Count} meal(s) added. Active until {result.ActiveUntil:HH:mm}";
                ResetForm();
                
                await Task.Delay(2000);
                Navigation.NavigateTo("/menus");
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

    public class CsvMealRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public sealed class CsvMealRowMap : ClassMap<CsvMealRow>
    {
        public CsvMealRowMap()
        {
            Map(m => m.Id).Index(0).Name("Id");
            Map(m => m.Name).Index(1).Name("Name");
            Map(m => m.Price).Index(2).Name("Price");
        }
    }
}