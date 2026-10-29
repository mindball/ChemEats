namespace Shared.DTOs.Reports;

public sealed record MenuReportOrderDto(
    string EmployeeName,
    string MealName,
    decimal Price
);
