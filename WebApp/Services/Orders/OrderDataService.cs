using Shared.DTOs.Orders;
using System.Net.Http.Json;

namespace WebApp.Services.Orders;


public class OrderDataService : IOrderDataService
{
    private readonly HttpClient _httpClient;

    public OrderDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequestDto requestDto)
    {
        if (requestDto is null) throw new ArgumentNullException(nameof(requestDto));

        var response = await _httpClient.PostAsJsonAsync("api/mealorders", requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        // server returns { Created = n, Ids = [...] }
        return await response.Content.ReadFromJsonAsync<PlaceOrdersResponse>();
    }

    public async Task<List<UserOrderDto>> GetMyOrdersAsync(Guid? supplierId = null, DateTime? date = null)
    {
        var query = new List<string>();
        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");
        if (date.HasValue)
            query.Add($"date={Uri.EscapeDataString(date.Value.ToString("o"))}"); // ISO format

        var url = "api/mealorders/me" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        var orders = await _httpClient.GetFromJsonAsync<List<UserOrderDto>>(url);
        return orders ?? new List<UserOrderDto>();
    }
}