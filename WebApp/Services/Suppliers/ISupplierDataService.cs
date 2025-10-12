using Shared.DTOs.Suppliers;

namespace WebApp.Services.Suppliers;

public interface ISupplierDataService
{
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();

    public Task<SupplierDto?> GetSupplierDetailsAsync(Guid id);

    Task<CreateSupplierDto?> AddSupplierAsync(CreateSupplierDto supplier);

    Task<UpdateSupplierDto?> UpdateSupplierAsync(UpdateSupplierDto supplier);

    Task DeleteSupplierAsync(Guid id);
}