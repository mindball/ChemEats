using System.Net.Http.Json;
using Shared;
using Shared.DTOs.Obligations;

namespace WebApp.Services.Obligations;

public class ObligationDataService : IObligationDataService
{
    private readonly HttpClient _httpClient;

    public ObligationDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<OtherObligationDto>?> GetMyObligationsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Obligations.MyObligations(startDate, endDate));

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<OtherObligationDto>>() ?? [];
    }

    public async Task<List<OtherObligationDto>?> GetUserObligationsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.AdminObligations.ByUser(userId, startDate, endDate));

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<OtherObligationDto>>() ?? [];
    }

    public async Task<OtherObligationDto?> CreateObligationAsync(CreateOtherObligationDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.AdminObligations.Base, requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OtherObligationDto>();
    }

    public async Task<OtherObligationDto?> UpdateObligationAsync(Guid obligationId, UpdateOtherObligationDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(ApiRoutes.AdminObligations.ById(obligationId), requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OtherObligationDto>();
    }

    public async Task<bool> DeleteObligationAsync(Guid obligationId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.AdminObligations.ById(obligationId));
        return response.IsSuccessStatusCode;
    }

    public async Task<ObligationPayResponseDto?> PayObligationsAsync(ObligationPayRequestDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.AdminObligations.PayObligations(), requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ObligationPayResponseDto>();
    }

    public async Task<ObligationPayResponseDto?> PayAllUnpaidAsync(ObligationPayAllRequestDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.AdminObligations.PayAllUnpaid(), requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ObligationPayResponseDto>();
    }
}
