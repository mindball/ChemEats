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

    
    public async Task<List<UserOrderDto>> GetMyOrdersAsync(Guid? supplierId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        List<string> query = [];
        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");
        if (startDate.HasValue)
        {
            // query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("o"))}");
            // query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToUniversalTime().ToString("o"))}"); //Not working
            query.Add($"startDate={startDate.Value:yyyy-MM-dd}");   //Working
            // query.Add($"startDate={ToQueryParam(startDate)}");    //Not working
        }
        if (endDate.HasValue)
        {
            // query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("o"))}");
            // query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToUniversalTime().ToString("o"))}");  //Not working
            query.Add($"endDate={endDate.Value:yyyy-MM-dd}"); //Working
            // query.Add($"endDate={ToQueryParam(endDate)}");  //Not working
        }

        string url = "api/mealorders/me" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        List<UserOrderDto>? orders = await _httpClient.GetFromJsonAsync<List<UserOrderDto>>(url);
        return orders ?? new List<UserOrderDto>();
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"api/mealorders/{orderId}");
        return response.IsSuccessStatusCode;
    }

    private static string? ToQueryParam(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        // Конвертирай към UTC, за да избегнеш timezone разлики
        var utc = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Local).ToUniversalTime();

        // ISO 8601 формат ("o") + Escape за безопасен URL
        return Uri.EscapeDataString(utc.ToString("o"));
    }

    public async Task<List<UserOrderItemDto>> GetMyOrderItemsAsync(Guid? supplierId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = new List<string>();
        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");
        if (startDate.HasValue)
            query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("o"))}");
        if (endDate.HasValue)
            query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("o"))}");

        var url = "api/mealorders/me/items" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        var items = await _httpClient.GetFromJsonAsync<List<UserOrderItemDto>>(url);
        return items ?? new List<UserOrderItemDto>();
    }
}