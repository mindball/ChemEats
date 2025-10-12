using Microsoft.AspNetCore.Components;
using Shared.DTOs.Suppliers;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Suppliers;

public class RegisterSupplierBase : ComponentBase
{
    [Inject] protected ISupplierDataService SupplierService { get; set; } = null!;

    protected CreateSupplierDto supplier = new() { Name = string.Empty };
    protected bool IsSubmitting;
    protected string? SuccessMessage;
    protected string? ErrorMessage;

    protected async Task RegisterSupplierAsync()
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;

        try
        {
            var createdSupplier = await SupplierService.AddSupplierAsync(supplier);

            if (createdSupplier is not null)
            {
                SuccessMessage = $"Supplier '{createdSupplier.Name}' registered successfully.";
                supplier = new CreateSupplierDto { Name = string.Empty }; // reset form
            }
            else
            {
                ErrorMessage = "Failed to register supplier.";
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