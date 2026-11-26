using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Obligations;
using WebApp.Services.Obligations;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.AdminPanel;

public class AdminObligationTypesBase : LocalizedComponentBase
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
                ErrorMessage = L["Failed to load the expense types. Please try again."];
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to load the expense types: {0}", ex.Message];
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
                ErrorMessage = L["Failed to add the expense type. The name may already exist."];
                return;
            }

            SuccessMessage = L["Expense type \"{0}\" added.", created.Name];
            NewName = null;
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to add the expense type: {0}", ex.Message];
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
                ErrorMessage = L["Failed to rename the expense type. The name may already exist."];
                return;
            }

            SuccessMessage = L["Expense type renamed to \"{0}\".", renamed.Name];
            CancelRename();
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to rename the expense type: {0}", ex.Message];
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
            L["Delete the expense type \"{0}\"? Existing obligations keep their text.", obligationType.Name].Value);

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
                ErrorMessage = L["Failed to delete the expense type. Please try again."];
                return;
            }

            SuccessMessage = L["Expense type \"{0}\" deleted.", obligationType.Name];
            if (EditingId == obligationType.Id)
                CancelRename();

            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Failed to delete the expense type: {0}", ex.Message];
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return L["Name is required."];

        if (name.Trim().Length > NameMaxLength)
            return L["Name cannot be longer than {0} characters.", NameMaxLength];

        return null;
    }
}
