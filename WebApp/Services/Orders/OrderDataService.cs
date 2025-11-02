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

    public async Task<PlaceOrdersResponse?> PlaceOrdersAsync(PlaceOrdersRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var response = await _httpClient.PostAsJsonAsync("api/mealorders", request);

        if (!response.IsSuccessStatusCode)
            return null;

        // server returns { Created = n, Ids = [...] }
        return await response.Content.ReadFromJsonAsync<PlaceOrdersResponse>();
    }
}