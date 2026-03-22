using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Suppliers;

public class EditSupplierBase : ComponentBase
{
    protected string? ErrorMessage;
    [Parameter] public bool IsSubmitting { get; set; }
    protected string? SuccessMessage;

    protected UpdateSupplierDto Supplier { get; set; } = new();
    protected List<EmployeeDto> AvailableUsers { get; set; } = [];
    protected bool CanEditSupervisorAssignment { get; private set; }

    [Inject] protected ISupplierDataService SupplierService { get; set; } = null!;
    [Inject] protected IEmployeeDataService EmployeeService { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Parameter] public Guid SupplierId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            CanEditSupervisorAssignment = authenticationState.User.IsInRole("Admin");

            SupplierDto? existing = await SupplierService.GetSupplierDetailsAsync(SupplierId);
            if (existing != null)
                Supplier = new UpdateSupplierDto
                {
                    Id = existing.Id,
                    City = existing.City,
                    Country = existing.Country,
                    VatNumber = existing.VatNumber,
                    Email = existing.Email,
                    PaymentTerms = existing.PaymentTerms,
                    Name = existing.Name,
                    Phone = existing.Phone,
                    PostalCode = existing.PostalCode,
                    StreetAddress = existing.StreetAddress,
                    SupervisorId = existing.SupervisorId,
                    Menus = existing.Menus
                };

            if (CanEditSupervisorAssignment)
                AvailableUsers = await EmployeeService.GetAllEmployeesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading supplier: {ex.Message}";
        }
    }

    protected async Task UpdateSupplierAsync()
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;

        try
        {
            UpdateSupplierDto? updated = await SupplierService.UpdateSupplierAsync(Supplier);

            if (updated != null)
            {
                Supplier = updated;
                SuccessMessage = $"Supplier '{updated.Name}' updated successfully.";
            }
            else
            {
                ErrorMessage = "Failed to update supplier.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}