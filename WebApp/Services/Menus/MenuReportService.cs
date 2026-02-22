namespace WebApp.Services.Menus;

public sealed class MenuReportService : IMenuReportService
{
    private readonly HttpClient _httpClient;

    public MenuReportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> GenerateMenuReportAsync(
        Guid menuId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetByteArrayAsync(
            $"api/reports/menu/{menuId}",
            cancellationToken);
    }
}