using System.Net.Http.Json;
using Shared.DTOs.Suppliers;

namespace WebApp.Services.Suppliers;

public class SupplierDataService : ISupplierDataService
{
    private readonly HttpClient _httpClient;

    public SupplierDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        IEnumerable<SupplierDto>? suppliers =
            await _httpClient.GetFromJsonAsync<IEnumerable<SupplierDto>>("api/suppliers");
        return suppliers ?? [];
    }

    public async Task<SupplierDto?> GetSupplierDetailsAsync(Guid id)
    {
        // if (string.IsNullOrWhiteSpace(id))
        //     return null;

        SupplierDto? supplier = await _httpClient.GetFromJsonAsync<SupplierDto>($"/api/suppliers/{id}");
        return supplier;
    }

    public async Task<CreateSupplierDto?> AddSupplierAsync(CreateSupplierDto supplier)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/suppliers", supplier);

        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<CreateSupplierDto>();

        string error = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Failed to add supplier: {error}");
    }

    public async Task<UpdateSupplierDto?> UpdateSupplierAsync(UpdateSupplierDto supplier)
    {
        // if (string.IsNullOrWhiteSpace(supplier.Id))
        //     throw new ArgumentException("Supplier Id is required for update.", nameof(supplier));

        SupplierDto? existing = await GetSupplierDetailsAsync(supplier.Id);

        if (existing is null) return null;

        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", supplier);

        if (!response.IsSuccessStatusCode)
            return null;

        UpdateSupplierDto? updatedSupplier = await response.Content.ReadFromJsonAsync<UpdateSupplierDto>();
        return updatedSupplier;
    }

    public async Task DeleteSupplierAsync(Guid id) // ✅ new
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"api/suppliers/{id}");
        response.EnsureSuccessStatusCode();
    }
}