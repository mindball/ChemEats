using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Orders;
using WebApp.Pages.Reports;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.Reports;

public class OutstandingReportPageTests : TestContext
{
    private static readonly DateTime StartDate = new(2026, 10, 1);
    private static readonly DateTime EndDate = new(2026, 10, 24);

    private readonly Mock<IOrderDataService> _orderDataServiceMock = new();

    public OutstandingReportPageTests()
    {
        Services.AddSingleton(_orderDataServiceMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void OutstandingReport_WhenSummariesLoaded_ShouldRenderPeriodRowsAndGrandTotal()
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate))
            .ReturnsAsync([
                new EmployeeOutstandingSummaryDto("u1", "Иван Петров", 3, 30m, 6m, 24m),
                new EmployeeOutstandingSummaryDto("u2", "Мария Иванова", 1, 9m, 3m, 6m)
            ]);

        IRenderedComponent<OutstandingReport> component = RenderReport(StartDate, EndDate);

        Assert.Equal("01.10.2026 – 24.10.2026", component.Find(".report-period").TextContent.Trim());

        List<string> rows = component.FindAll("table.report-table tbody tr")
            .Select(row => string.Join("|", row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())))
            .ToList();
        Assert.Equal(
        [
            $"1|Иван Петров|3|{30m:0.00 €}|{6m:0.00 €}|{24m:0.00 €}|{0m:0.00 €}|{0m:0.00 €}|{24m:0.00 €}",
            $"2|Мария Иванова|1|{9m:0.00 €}|{3m:0.00 €}|{6m:0.00 €}|{0m:0.00 €}|{0m:0.00 €}|{6m:0.00 €}"
        ], rows);

        IElement totalRow = component.Find("table.report-table tfoot tr");
        Assert.Contains("Grand Total", totalRow.TextContent);
        Assert.Contains(30m.ToString("0.00 €"), component.Find(".report-total-amount").TextContent);
    }

    [Fact]
    public void OutstandingReport_WhenEmployeeHasOtherObligations_ShouldShowOtherColumnAndIncludeItInTotals()
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate))
            .ReturnsAsync([
                new EmployeeOutstandingSummaryDto("u1", "Иван Петров", 2, 20m, 6m, 14m, 1, 4m, OtherUnpaidCount: 2, OtherAmount: 15.5m),
                new EmployeeOutstandingSummaryDto("u2", "Мария Иванова", 0, 0m, 0m, 0m, OtherUnpaidCount: 1, OtherAmount: 8m)
            ]);

        IRenderedComponent<OutstandingReport> component = RenderReport(StartDate, EndDate);

        List<string> rows = component.FindAll("table.report-table tbody tr")
            .Select(row => string.Join("|", row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())))
            .ToList();
        Assert.Equal(
        [
            $"1|Иван Петров|2|{20m:0.00 €}|{6m:0.00 €}|{14m:0.00 €}|{4m:0.00 €}|{15.5m:0.00 €}|{33.5m:0.00 €}",
            $"2|Мария Иванова|0|{0m:0.00 €}|{0m:0.00 €}|{0m:0.00 €}|{0m:0.00 €}|{8m:0.00 €}|{8m:0.00 €}"
        ], rows);
        Assert.Contains(41.5m.ToString("0.00 €"), component.Find(".report-total-amount").TextContent);
    }

    [Fact]
    public void OutstandingReport_WhenPeriodIsMissing_ShouldShowErrorAndNotCallService()
    {
        IRenderedComponent<OutstandingReport> component = RenderComponent<OutstandingReport>();

        Assert.Contains("The report period is missing", component.Find(".report-message").TextContent);
        _orderDataServiceMock.Verify(
            service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public void OutstandingReport_WhenServiceFails_ShouldShowError()
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate))
            .ReturnsAsync((List<EmployeeOutstandingSummaryDto>?)null);

        IRenderedComponent<OutstandingReport> component = RenderReport(StartDate, EndDate);

        Assert.Contains("could not be loaded", component.Find(".report-message").TextContent);
    }

    [Fact]
    public void PrintButton_WhenClicked_ShouldInvokeBrowserPrint()
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate))
            .ReturnsAsync([]);

        IRenderedComponent<OutstandingReport> component = RenderReport(StartDate, EndDate);

        component.Find("button.btn-primary").Click();

        JSInterop.VerifyInvoke("print");
    }

    [Fact]
    public void BackButton_WhenClicked_ShouldReturnToSummaryWithSamePeriod()
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate))
            .ReturnsAsync([]);

        IRenderedComponent<OutstandingReport> component = RenderReport(StartDate, EndDate);

        component.Find("button.btn-outline-secondary").Click();

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/admin/payments/summary?startDate=2026-10-01&endDate=2026-10-24", navigationManager.Uri);
    }

    [Fact]
    public void OutstandingReport_WhenAllDatesIsSet_ShouldLoadAllUnpaid()
    {
        _orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync(null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Иван Петров", 2, 20m, 0m, 20m)]);

        Services.GetRequiredService<NavigationManager>().NavigateTo(OutstandingReportBase.AllDatesPageUrl);
        IRenderedComponent<OutstandingReport> component = RenderComponent<OutstandingReport>();

        Assert.Equal("All dates", component.Find(".report-period").TextContent.Trim());
        Assert.Contains(20m.ToString("0.00 €"), component.Find(".report-total-amount").TextContent);
    }

    private IRenderedComponent<OutstandingReport> RenderReport(DateTime startDate, DateTime endDate)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(OutstandingReportBase.PageUrl(startDate, endDate));
        return RenderComponent<OutstandingReport>();
    }
}
