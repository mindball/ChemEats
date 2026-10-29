using Shared.DTOs.Reports;

namespace WebApp.Services.Menus;

public interface IMenuReportService
{
    Task<MenuReportDto?> GetMenuReportAsync(
        Guid menuId,
        CancellationToken cancellationToken = default);
}
