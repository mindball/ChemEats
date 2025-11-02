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
}