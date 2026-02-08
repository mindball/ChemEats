using Microsoft.AspNetCore.Components;
using Shared.DTOs.Suppliers;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Suppliers;

public class RegisterSupplierBase : ComponentBase
{
    [Inject] protected ISupplierDataService SupplierService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected CreateSupplierDto supplier = new()
    {
        Name = string.Empty,
        VatNumber = string.Empty
    };

    protected bool IsSubmitting { get; set; }
    protected string? SuccessMessage { get; set; }
    protected string? ErrorMessage { get; set; }

    protected async Task RegisterSupplierAsync()
    {
        try
        {
            IsSubmitting = true;
            SuccessMessage = null;
            ErrorMessage = null;

            CreateSupplierDto? createdSupplier = await SupplierService.AddSupplierAsync(supplier);

            if (createdSupplier is not null)
            {
                SuccessMessage = $"Supplier '{createdSupplier.Name}' registered successfully!";
                ResetForm();

                await Task.Delay(2000);
                Navigation.NavigateTo("/get-suppliers");
            }
            else
            {
                ErrorMessage = "Failed to register supplier.";
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

    protected void ResetForm()
    {
        supplier = new CreateSupplierDto
        {
            Name = string.Empty,
            VatNumber = string.Empty
        };
        ErrorMessage = null;
        SuccessMessage = null;
    }
}