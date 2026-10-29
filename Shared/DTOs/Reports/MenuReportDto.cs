namespace Shared.DTOs.Reports;

public sealed record MenuReportDto(
    Guid MenuId,
    string SupplierName,
    DateTime MenuDate,
    DateTime ActiveUntil,
    IReadOnlyList<MenuReportOrderDto> Orders
);
