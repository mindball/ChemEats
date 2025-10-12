using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.DTOs.Suppliers;
using WebApp.Services.Suppliers;

namespace WebApp.Pages.Suppliers;

public class GetSuppliersBase : ComponentBase
{
    [Inject] protected ISupplierDataService SupplierService { get; set; } = null!;

    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;


    protected List<SupplierDto> Suppliers { get; set; } = [];
    protected SupplierDto? SelectedSupplier { get; set; }
    protected bool IsViewModalOpen { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadSuppliersAsync();
    }

    protected async Task LoadSuppliersAsync()
    {
        try
        {
            IEnumerable<SupplierDto> suppliers = await SupplierService.GetAllSuppliersAsync();
            Suppliers = suppliers.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading suppliers: {ex.Message}");
        }
    }

    protected void ViewSupplier(SupplierDto supplier)
    {
        SelectedSupplier = supplier;
        IsViewModalOpen = true;
    }

    protected void CloseViewModal()
    {
        SelectedSupplier = null;
        IsViewModalOpen = false;
    }


    protected void UpdateSupplier(SupplierDto supplier)
    {
        NavigationManager.NavigateTo($"/edit-supplier/{supplier.Id}");
    }

    protected async Task DeleteSupplierAsync(SupplierDto supplier)
    {
        if (!await ConfirmDeleteAsync(supplier))
            return;

        try
        {
            if (supplier.Id != null) await SupplierService.DeleteSupplierAsync(supplier.Id);
            Suppliers.Remove(supplier);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting supplier: {ex.Message}");
        }
    }

    protected async Task<bool> ConfirmDeleteAsync(SupplierDto supplier)
    {
        return await ConfirmDelete($"Are you sure you want to delete supplier '{supplier.Name}'?");
    }

    private async Task<bool> ConfirmDelete(string message)
    {
        return await JSRuntime.InvokeAsync<bool>("confirm", message);
    }
}