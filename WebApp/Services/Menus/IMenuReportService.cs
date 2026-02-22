namespace WebApp.Services.Menus;

public interface IMenuReportService
{
    Task<byte[]> GenerateMenuReportAsync(
        Guid menuId,
        CancellationToken cancellationToken = default);
}