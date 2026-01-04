using Shared.DTOs.Errors;
using Shared.DTOs.Menus;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Services.Menus;

public class MenuDataService : IMenuDataService
{
    private readonly HttpClient _httpClient;

    public MenuDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MenuDto?> AddMenuAsync(CreateMenuDto menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/menus", menu);

        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<MenuDto>();

        string error = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Failed to add menu: {error}");
    }

    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync(bool includeDeleted)
    {
        string url = includeDeleted ? "api/menus?includeDeleted=true" : "api/menus";
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Failed to get menus: {error}");
        }

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    public async Task<IEnumerable<MenuDto>> GetMenusBySupplierAsync(string supplierId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"api/menus/supplier/{supplierId}");

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Failed to get menus: {error}");
        }

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    public async Task<bool> UpdateMenuDateAsync(Guid menuId, DateTime newDate)
    {
        HttpResponseMessage resp = await _httpClient.PutAsJsonAsync($"api/menus/{menuId}/date", newDate);
        return resp.IsSuccessStatusCode;
    }

    // public async Task<bool> DeactivateMenuAsync(Guid menuId)
    // {
    //     HttpResponseMessage resp = await _httpClient.PostAsync($"api/menus/{menuId}/deactivate", content: null);
    //     return resp.IsSuccessStatusCode;
    // }
    //
    // public async Task<bool> ActivateMenuAsync(Guid menuId)
    // {
    //     HttpResponseMessage resp = await _httpClient.PostAsync($"api/menus/{menuId}/activate", content: null);
    //     return resp.IsSuccessStatusCode;
    // }

    public async Task<bool> SoftDeleteMenuAsync(Guid menuId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"api/menus/{menuId}");

        if (!response.IsSuccessStatusCode)
        {
            ProblemDetailsDto? problem = await SafeReadJsonAsync<ProblemDetailsDto>(response);
            if (problem is not null && !string.IsNullOrWhiteSpace(problem.Detail))
                throw new ApplicationException(problem.Detail);

            ErrorResponse? error = await SafeReadJsonAsync<ErrorResponse>(response);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Message))
                throw new ApplicationException(error.Message);

            string raw = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(string.IsNullOrWhiteSpace(raw)
                ? $"Delete failed ({(int)response.StatusCode})"
                : raw);
        }

        return true;
    }

    private static async Task<T?> SafeReadJsonAsync<T>(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }
}