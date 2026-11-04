using System.Net.Http.Json;
using Shared;
using Shared.DTOs.Obligations;

namespace WebApp.Services.Obligations;

public class ObligationTypeDataService : IObligationTypeDataService
{
    private readonly HttpClient _httpClient;

    public ObligationTypeDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ObligationTypeDto>?> GetObligationTypesAsync()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.AdminObligationTypes.Base);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<ObligationTypeDto>>() ?? [];
    }

    public async Task<ObligationTypeDto?> CreateObligationTypeAsync(string name)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            ApiRoutes.AdminObligationTypes.Base,
            new SaveObligationTypeDto(name));

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ObligationTypeDto>();
    }

    public async Task<ObligationTypeDto?> RenameObligationTypeAsync(Guid obligationTypeId, string name)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
            ApiRoutes.AdminObligationTypes.ById(obligationTypeId),
            new SaveObligationTypeDto(name));

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ObligationTypeDto>();
    }

    public async Task<bool> DeleteObligationTypeAsync(Guid obligationTypeId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.AdminObligationTypes.ById(obligationTypeId));
        return response.IsSuccessStatusCode;
    }
}
