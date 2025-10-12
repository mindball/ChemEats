using System.Net.Http.Json;
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
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/menus", menu);

        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<MenuDto>();

        string error = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Failed to add menu: {error}");
    }

    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("api/menus");

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Failed to get menus: {error}");
        }

        IEnumerable<MenuDto>? menus = await response.Content.ReadFromJsonAsync<IEnumerable<MenuDto>>();
        return menus ?? [];
    }

    // Optional: fetch menus by supplier
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
}