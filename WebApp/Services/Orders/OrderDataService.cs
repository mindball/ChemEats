using System.Net.Http.Json;
using Shared.DTOs.Orders;

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
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/mealorders", requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<PlaceOrdersResponse>();
    }

    public async Task<List<UserOrderDto>> GetMyOrdersAsync(Guid? supplierId = null, DateTime? startDate = null,
        DateTime? endDate = null)
    {
        List<string> query = [];
        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");
        if (startDate.HasValue)
            query.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            query.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        string url = "api/mealorders/me" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        List<UserOrderDto>? orders = await _httpClient.GetFromJsonAsync<List<UserOrderDto>>(url);
        return orders ?? [];
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"api/mealorders/{orderId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserOrderItemDto>> GetMyOrderItemsAsync(
        Guid? supplierId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeDeleted = false,
        string? status = null)
    {
        List<string> query = [$"includeDeleted={includeDeleted}"];

        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");

        if (startDate.HasValue)
            query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("o"))}");

        if (endDate.HasValue)
            query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("o"))}");

        if (!string.IsNullOrWhiteSpace(status))
            query.Add($"status={Uri.EscapeDataString(status)}");

        string url = "api/mealorders/me/items?" + string.Join("&", query);

        List<UserOrderItemDto>? items = await _httpClient.GetFromJsonAsync<List<UserOrderItemDto>>(url);
        return items ?? [];
    }

    public async Task<List<UserOrderPaymentItemDto>> GetMyUnpaidPaymentsAsync(Guid? supplierId = null)
    {
        string url = "api/mealorders/me/payments";
        if (supplierId.HasValue)
            url += $"?supplierId={supplierId.Value}";

        List<UserOrderPaymentItemDto>? items = await _httpClient.GetFromJsonAsync<List<UserOrderPaymentItemDto>>(url);
        return items ?? [];
    }

    public async Task<UserOutstandingSummaryDto?> GetMyPaymentsSummaryAsync()
    {
        return await _httpClient.GetFromJsonAsync<UserOutstandingSummaryDto>("api/mealorders/me/payments/summary");
    }

    public async Task<bool> MarkOrderAsPaidAsync(Guid orderId)
    {
        HttpResponseMessage response = await _httpClient.PatchAsync($"api/mealorders/{orderId}/pay", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserOrderPaymentItemDto>> GetUnpaidOrdersByUserAsync(string userId, Guid? supplierId = null)
    {
        string url = $"api/admin/mealorders/unpaid/{Uri.EscapeDataString(userId)}";
        if (supplierId.HasValue)
            url += $"?supplierId={supplierId.Value}";

        List<UserOrderPaymentItemDto>? items = await _httpClient.GetFromJsonAsync<List<UserOrderPaymentItemDto>>(url);
        return items ?? [];
    }

    public async Task<List<UserOrderPaymentItemDto>> GetOrdersByUserForPeriodAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, Guid? supplierId = null)
    {
        List<string> query = [];

        if (startDate.HasValue)
            query.Add($"startDate={startDate.Value:yyyy-MM-dd}");

        if (endDate.HasValue)
            query.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        if (supplierId.HasValue)
            query.Add($"supplierId={supplierId.Value}");

        string url = $"api/admin/mealorders/period/{Uri.EscapeDataString(userId)}" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        List<UserOrderPaymentItemDto>? items = await _httpClient.GetFromJsonAsync<List<UserOrderPaymentItemDto>>(url);
        return items ?? [];
    }

    public async Task<OrderPayResponseDto?> OrderMarkAsPaidAsync(OrderPayRequestDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/admin/mealorders/order-pay", requestDto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OrderPayResponseDto>();
    }

    public async Task<List<UserOrderItemDto>> GetMyOrdersByMenuAsync(Guid menuId)
    {
        List<UserOrderItemDto>? items =
            await _httpClient.GetFromJsonAsync<List<UserOrderItemDto>>($"api/mealorders/me/menu/{menuId}");
        return items ?? [];
    }
}