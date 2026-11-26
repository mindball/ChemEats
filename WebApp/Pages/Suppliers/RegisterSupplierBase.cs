using Microsoft.AspNetCore.Components;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Suppliers;

public class RegisterSupplierBase : LocalizedComponentBase
{
    [Inject] protected ISupplierDataService SupplierService { get; set; } = default!;
    [Inject] protected IEmployeeDataService EmployeeService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected CreateSupplierDto supplier = new()
    {
        Name = string.Empty,
        VatNumber = string.Empty
    };

    protected List<EmployeeDto> AvailableUsers { get; set; } = [];
    protected bool IsSubmitting { get; set; }
    protected string? SuccessMessage { get; set; }
    protected string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            AvailableUsers = await EmployeeService.GetAllEmployeesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = L["Error loading users: {0}", ex.Message];
        }
    }

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
                SuccessMessage = L["Supplier '{0}' registered successfully!", createdSupplier.Name];
                ResetForm();

                await Task.Delay(2000);
                Navigation.NavigateTo("/get-suppliers");
            }
            else
            {
                ErrorMessage = L["Failed to register supplier."];
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