using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Obligations;
using WebApp.Services.Obligations;

namespace WebApp.Pages.AdminPanel;

public class AdminObligationTypesBase : ComponentBase
{
    public const string BasePageUrl = "/admin/payments/obligation-types";
    public const int NameMaxLength = 150;

    [Inject] protected IObligationTypeDataService ObligationTypeDataService { get; init; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; init; } = null!;

    protected List<ObligationTypeDto>? ObligationTypes { get; private set; }

    protected bool IsLoading { get; private set; }
    protected bool IsProcessing { get; private set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected string? NewName { get; set; }

    // Inline rename; EditingId is null when no row is being renamed.
    protected Guid? EditingId { get; private set; }
    protected string? EditingName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadTypesAsync();
    }

    protected async Task LoadTypesAsync()
    {
        IsLoading = true;

        try
        {
            ObligationTypes = await ObligationTypeDataService.GetObligationTypesAsync();

            if (ObligationTypes is null)
                ErrorMessage = "Failed to load the expense types. Please try again.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load the expense types: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task AddAsync()
    {
        if (IsProcessing)
            return;

        string? validationError = ValidateName(NewName);
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
            ObligationTypeDto? created = await ObligationTypeDataService.CreateObligationTypeAsync(NewName!.Trim());
            if (created is null)
            {
                ErrorMessage = "Failed to add the expense type. The name may already exist.";
                return;
            }

            SuccessMessage = $"Expense type \"{created.Name}\" added.";
            NewName = null;
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add the expense type: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected void StartRename(ObligationTypeDto obligationType)
    {
        EditingId = obligationType.Id;
        EditingName = obligationType.Name;
    }

    protected void CancelRename()
    {
        EditingId = null;
        EditingName = null;
    }

    protected async Task SaveRenameAsync()
    {
        if (!EditingId.HasValue || IsProcessing)
            return;

        string? validationError = ValidateName(EditingName);
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
            ObligationTypeDto? renamed = await ObligationTypeDataService.RenameObligationTypeAsync(EditingId.Value, EditingName!.Trim());
            if (renamed is null)
            {
                ErrorMessage = "Failed to rename the expense type. The name may already exist.";
                return;
            }

            SuccessMessage = $"Expense type renamed to \"{renamed.Name}\".";
            CancelRename();
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to rename the expense type: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task DeleteAsync(ObligationTypeDto obligationType)
    {
        if (IsProcessing)
            return;

        bool confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Delete the expense type \"{obligationType.Name}\"? Existing obligations keep their text.");

        if (!confirmed)
            return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            bool deleted = await ObligationTypeDataService.DeleteObligationTypeAsync(obligationType.Id);
            if (!deleted)
            {
                ErrorMessage = "Failed to delete the expense type. Please try again.";
                return;
            }

            SuccessMessage = $"Expense type \"{obligationType.Name}\" deleted.";
            if (EditingId == obligationType.Id)
                CancelRename();

            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete the expense type: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";

        if (name.Trim().Length > NameMaxLength)
            return $"Name cannot be longer than {NameMaxLength} characters.";

        return null;
    }
}
