using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Obligations;
using WebApp.Services.Employees;
using WebApp.Services.Obligations;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.AdminPanel;

public class AdminObligationsBase : LocalizedComponentBase
{
    public const string BasePageUrl = "/admin/payments/obligations";

    public static string EmployeeUnpaidPageUrl(string userId, DateTime? upToDate) =>
        upToDate.HasValue
            ? $"{BasePageUrl}?userId={Uri.EscapeDataString(userId)}&endDate={upToDate.Value:yyyy-MM-dd}"
            : $"{BasePageUrl}?userId={Uri.EscapeDataString(userId)}&allDates=true";

    [Inject] protected IObligationDataService ObligationDataService { get; init; } = null!;
    [Inject] protected IObligationTypeDataService ObligationTypeDataService { get; init; } = null!;
    [Inject] protected IEmployeeDataService EmployeeDataService { get; init; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; init; } = null!;

    [SupplyParameterFromQuery(Name = "userId")] public string? QueryUserId { get; set; }
    [SupplyParameterFromQuery(Name = "endDate")] public DateTime? QueryEndDate { get; set; }
    [SupplyParameterFromQuery(Name = "allDates")] public bool? QueryAllDates { get; set; }

    protected List<EmployeeDto>? AllEmployees { get; private set; }
    protected List<ObligationTypeDto> ObligationTypes { get; private set; } = [];
    protected List<EmployeeDto> FilteredEmployees => FilterEmployees();
    protected string? SearchTerm { get; set; }
    protected string? SelectedUserId { get; set; }

    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }

    // Ignore the date filter and show every obligation of the employee.
    protected bool AllDates { get; set; }
    protected string PaymentStatusFilter { get; set; } = "All";

    protected List<OtherObligationDto>? Obligations { get; private set; }
    protected List<OtherObligationDto> FilteredObligations => FilterByPaymentStatus();
    protected HashSet<Guid> SelectedIds { get; } = [];

    // The employee and period the current list belongs to; the filters may have been changed since.
    protected string? LoadedUserId { get; private set; }
    protected DateTime? LoadedEndDate { get; private set; }
    protected bool LoadedAllDates { get; private set; }

    protected bool IsLoading { get; private set; }
    protected bool IsProcessing { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    // Add / edit form; EditingId is null while adding a new obligation.
    protected Guid? EditingId { get; private set; }
    protected Guid? FormTypeId { get; set; }

    // String view of FormTypeId for the <select> binding; an empty value means "not selected".
    protected string FormTypeValue
    {
        get => FormTypeId?.ToString() ?? string.Empty;
        set => FormTypeId = Guid.TryParse(value, out Guid id) ? id : null;
    }
    protected decimal? FormAmount { get; set; }
    protected DateTime? FormDate { get; set; }

    protected bool HasSelection => SelectedIds.Count > 0;
    protected string LoadedEmployeeName =>
        AllEmployees?.FirstOrDefault(e => e.UserId == LoadedUserId)?.FullName ?? LoadedUserId ?? string.Empty;

    protected static bool IsUnpaid(OtherObligationDto obligation) =>
        obligation.PaymentStatus == PaymentStatusDto.Unpaid;

    protected int TotalCount => Obligations?.Count ?? 0;
    protected int UnpaidCount => Obligations?.Count(IsUnpaid) ?? 0;
    protected int PaidCount => TotalCount - UnpaidCount;
    protected decimal UnpaidTotal => Obligations?.Where(IsUnpaid).Sum(o => o.Amount) ?? 0m;
    protected decimal PaidTotal => Obligations?.Where(o => !IsUnpaid(o)).Sum(o => o.Amount) ?? 0m;
    protected decimal SelectedTotal => Obligations?.Where(o => SelectedIds.Contains(o.Id)).Sum(o => o.Amount) ?? 0m;

    protected string PayAllScopeText =>
        LoadedAllDates || !LoadedEndDate.HasValue ? L["regardless of date"] : L["up to {0:dd.MM.yyyy}", LoadedEndDate.GetValueOrDefault()];

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        try
        {
            List<EmployeeDto> employees = await EmployeeDataService.GetAllEmployeesAsync();
            AllEmployees = employees.OrderBy(e => e.FullName).ToList();

            List<ObligationTypeDto>? types = await ObligationTypeDataService.GetObligationTypesAsync();
            if (types is null)
                ErrorMessage = L["Failed to load the expense types."];
            ObligationTypes = types ?? [];

            // Default: current month
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = QueryEndDate ?? StartDate.Value.AddMonths(1).AddDays(-1);
            ResetForm();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to load employees: {0}", ex.Message];
        }
        finally
        {
            IsLoading = false;
        }

        // Deep link from the Outstanding Summary: open an employee's unpaid obligations up to the period end
        // (no lower bound, like the summary) or regardless of date.
        if (!string.IsNullOrWhiteSpace(QueryUserId))
        {
            SelectedUserId = QueryUserId;
            AllDates = QueryAllDates == true || !QueryEndDate.HasValue;
            if (!AllDates)
                StartDate = null;
            PaymentStatusFilter = "Unpaid";
            await SearchAsync();
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

    private List<OtherObligationDto> FilterByPaymentStatus()
    {
        if (Obligations is null)
            return [];

        return PaymentStatusFilter switch
        {
            "Paid" => Obligations.Where(o => !IsUnpaid(o)).ToList(),
            "Unpaid" => Obligations.Where(IsUnpaid).ToList(),
            _ => Obligations
        };
    }

    protected async Task SearchAsync()
    {
        SuccessMessage = null;
        ResetForm();
        await LoadObligationsAsync();
    }

    // Keeps SuccessMessage so a confirmation survives the refresh that follows it.
    private async Task LoadObligationsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ErrorMessage = L["Please select an employee."];
            return;
        }

        if (!AllDates && StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
        {
            ErrorMessage = L["End date cannot be before start date."];
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        Obligations = null;
        SelectedIds.Clear();

        try
        {
            List<OtherObligationDto>? obligations = AllDates
                ? await ObligationDataService.GetUserObligationsAsync(SelectedUserId)
                : await ObligationDataService.GetUserObligationsAsync(SelectedUserId, StartDate, EndDate);

            if (obligations is null)
            {
                ErrorMessage = L["Failed to load the obligations. Please try again."];
                return;
            }

            Obligations = obligations;
            LoadedUserId = SelectedUserId;
            LoadedAllDates = AllDates;
            LoadedEndDate = AllDates ? null : EndDate?.Date;
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to load the obligations: {0}", ex.Message];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void StartEdit(OtherObligationDto obligation)
    {
        if (!IsUnpaid(obligation))
            return;

        EditingId = obligation.Id;
        // A deleted (or missing) type is not in the list, so the admin has to pick a current one.
        FormTypeId = ObligationTypes.Any(t => t.Id == obligation.ObligationTypeId) ? obligation.ObligationTypeId : null;
        FormAmount = obligation.Amount;
        FormDate = obligation.Date.Date;
    }

    protected void ResetForm()
    {
        EditingId = null;
        FormTypeId = null;
        FormAmount = null;
        FormDate = DateTime.Today;
    }

    protected string? ValidateForm()
    {
        if (!FormTypeId.HasValue || ObligationTypes.All(t => t.Id != FormTypeId.Value))
            return L["Please select an expense type."];

        if (!FormAmount.HasValue || FormAmount.Value <= 0m)
            return L["Amount must be greater than zero."];

        if (decimal.Round(FormAmount.Value, 2) != FormAmount.Value)
            return L["Amount cannot have more than two decimal places."];

        if (!FormDate.HasValue)
            return L["Date is required."];

        return null;
    }

    protected async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(LoadedUserId) || IsProcessing)
            return;

        string? validationError = ValidateForm();
        if (validationError is not null)
        {
            ErrorMessage = validationError;
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            Guid typeId = FormTypeId!.Value;
            OtherObligationDto? saved = EditingId.HasValue
                ? await ObligationDataService.UpdateObligationAsync(
                    EditingId.Value,
                    new UpdateOtherObligationDto(typeId, FormAmount!.Value, FormDate!.Value.Date))
                : await ObligationDataService.CreateObligationAsync(
                    new CreateOtherObligationDto(LoadedUserId, typeId, FormAmount!.Value, FormDate!.Value.Date));

            if (saved is null)
            {
                ErrorMessage = L["Failed to save the obligation. Paid obligations cannot be changed."];
                return;
            }

            SuccessMessage = EditingId.HasValue
                ? L["Obligation \"{0}\" updated.", saved.Description]
                : L["Obligation \"{0}\" ({1}) added for {2}.", saved.Description, FormatEuro(saved.Amount), LoadedEmployeeName];

            ResetForm();
            await LoadObligationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to save the obligation: {0}", ex.Message];
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task DeleteAsync(OtherObligationDto obligation)
    {
        if (!IsUnpaid(obligation) || IsProcessing)
            return;

        bool confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            L["Delete the obligation \"{0}\" ({1})?", obligation.Description, FormatEuro(obligation.Amount)].Value);

        if (!confirmed)
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            bool deleted = await ObligationDataService.DeleteObligationAsync(obligation.Id);
            if (!deleted)
            {
                ErrorMessage = L["Failed to delete the obligation. Paid obligations cannot be deleted."];
                return;
            }

            SuccessMessage = L["Obligation \"{0}\" deleted.", obligation.Description];
            if (EditingId == obligation.Id)
                ResetForm();

            await LoadObligationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to delete the obligation: {0}", ex.Message];
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected void ToggleSelection(Guid obligationId, bool isSelected)
    {
        if (isSelected)
            SelectedIds.Add(obligationId);
        else
            SelectedIds.Remove(obligationId);
    }

    protected async Task PaySelectedAsync()
    {
        if (!HasSelection || string.IsNullOrWhiteSpace(LoadedUserId) || IsProcessing)
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            ObligationPayResponseDto? result = await ObligationDataService.PayObligationsAsync(
                new ObligationPayRequestDto(LoadedUserId, SelectedIds.ToList()));

            if (result is null)
            {
                ErrorMessage = L["Failed to process the payment. Please try again."];
                return;
            }

            SuccessMessage = L["{0} obligations marked as paid. Total: {1}", result.PaidCount, FormatEuro(result.TotalPaid)];
            await LoadObligationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Payment processing failed: {0}", ex.Message];
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task PayAllUnpaidAsync()
    {
        if (string.IsNullOrWhiteSpace(LoadedUserId) || UnpaidCount == 0 || IsProcessing)
            return;

        bool confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            L["Mark all unpaid obligations of {0} {1} as paid?", LoadedEmployeeName, PayAllScopeText].Value);

        if (!confirmed)
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            ObligationPayResponseDto? result = await ObligationDataService.PayAllUnpaidAsync(
                new ObligationPayAllRequestDto(LoadedUserId, LoadedAllDates ? null : LoadedEndDate));

            if (result is null)
            {
                ErrorMessage = L["Failed to process the payment. Please try again."];
                return;
            }

            SuccessMessage = L["{0} obligations marked as paid. Total: {1}", result.PaidCount, FormatEuro(result.TotalPaid)];
            await LoadObligationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Payment processing failed: {0}", ex.Message];
        }
        finally
        {
            IsProcessing = false;
        }
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
