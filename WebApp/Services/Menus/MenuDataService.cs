using System.Net.Http.Json;
using System.Text.Json;
using Shared.DTOs.Errors;
using Shared.DTOs.Menus;

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
        ArgumentNullException.ThrowIfNull(menu);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/menus", menu);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<MenuDto>();

        await ThrowDetailedExceptionAsync(response, "Failed to add menu");
        return null;
    }

    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync(bool includeDeleted = false)
    {
        string url = includeDeleted ? "api/menus?includeDeleted=true" : "api/menus";
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, "Failed to get menus");

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    public async Task<IEnumerable<MenuDto>> GetActiveMenusAsync()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("api/menus/active");

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, "Failed to get active menus");

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    public async Task<IEnumerable<MenuDto>> GetMenusBySupplierAsync(Guid supplierId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"api/menus/supplier/{supplierId}");

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, "Failed to get menus by supplier");

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    public async Task<bool> UpdateMenuDateAsync(Guid menuId, DateTime newDate)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/menus/{menuId}/date", newDate);

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, $"Failed to update menu {menuId} date");

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateMenuActiveUntilAsync(Guid menuId, DateTime newActiveUntil)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/menus/{menuId}/active-until", newActiveUntil);

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, $"Failed to update menu {menuId} active until");

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SoftDeleteMenuAsync(Guid menuId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"api/menus/{menuId}");

        if (!response.IsSuccessStatusCode)
            await ThrowDetailedExceptionAsync(response, $"Failed to delete menu {menuId}");

        return true;
    }

    private static async Task ThrowDetailedExceptionAsync(HttpResponseMessage response, string defaultMessage)
    {
        ProblemDetailsDto? problem = await SafeReadJsonAsync<ProblemDetailsDto>(response);
        if (problem is not null && !string.IsNullOrWhiteSpace(problem.Detail))
            throw new ApplicationException(problem.Detail);

        ErrorResponse? error = await SafeReadJsonAsync<ErrorResponse>(response);
        if (error is not null && !string.IsNullOrWhiteSpace(error.Message))
            throw new ApplicationException(error.Message);

        string raw = await response.Content.ReadAsStringAsync();
        string message = string.IsNullOrWhiteSpace(raw)
            ? $"{defaultMessage} ({(int)response.StatusCode})"
            : $"{defaultMessage}: {raw}";

        throw new ApplicationException(message);
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