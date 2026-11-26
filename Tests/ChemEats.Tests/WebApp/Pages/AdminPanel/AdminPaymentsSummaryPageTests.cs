using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Orders;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminPaymentsSummaryPageTests : LocalizedTestContext
{
    [Fact]
    public void AdminPaymentsSummary_WhenSummariesLoaded_ShouldRenderEmployeeRowsAndGrandTotal()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([
                new EmployeeOutstandingSummaryDto("u1", "Alpha User", 3, 30m, 6m, 24m),
                new EmployeeOutstandingSummaryDto("u2", "Beta User", 1, 9m, 3m, 6m)
            ]);

        Services.AddSingleton(orderDataServiceMock.Object);

        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
        {
            IRefreshableElementCollection<IElement> rows = component.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            Assert.Contains("Alpha User", rows[0].TextContent);
            Assert.Contains("Beta User", rows[1].TextContent);

            IElement totalRow = component.Find("tfoot tr");
            Assert.Contains("Grand Total", totalRow.TextContent);
            Assert.Equal("4", totalRow.QuerySelectorAll("td")[1].TextContent.Trim());
        });
    }

    [Fact]
    public void AdminPaymentsSummary_WhenNoUnpaidOrders_ShouldRenderEmptyMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        Services.AddSingleton(orderDataServiceMock.Object);

        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("No unpaid orders up to the end of this period", component.Markup);
            Assert.Empty(component.FindAll("table"));
        });
    }

    [Fact]
    public void AdminPaymentsSummary_WhenPeriodIsInQuery_ShouldLoadThatPeriodAndShowIt()
    {
        DateTime startDate = new(2026, 9, 1);
        DateTime endDate = new(2026, 9, 30);

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(startDate, endDate))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m)]);

        Services.AddSingleton(orderDataServiceMock.Object);

        Services.GetRequiredService<NavigationManager>().NavigateTo(AdminPaymentsSummaryBase.PageUrl(startDate, endDate));
        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
            Assert.Contains("Period: 01.09.2026 – 30.09.2026", component.Find(".outstanding-period").TextContent));
        orderDataServiceMock.Verify(service => service.GetOutstandingSummaryAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public void AdminPaymentsSummary_WhenEmployeeHasEarlierDebt_ShouldShowEarlierAndTotalDue()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m, 2, 5m)]);

        Services.AddSingleton(orderDataServiceMock.Object);

        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
        {
            IElement row = component.Find("tbody tr");
            List<string> cells = row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()).ToList();
            Assert.Contains("(2)", cells[5]);
            Assert.Equal("—", cells[6]);
            Assert.Contains("12", cells[7]);
        });
    }

    [Fact]
    public void AdminPaymentsSummary_WhenEmployeeHasOtherObligations_ShouldShowLinkAndIncludeThemInTotalDue()
    {
        DateTime startDate = new(2026, 9, 1);
        DateTime endDate = new(2026, 9, 30);

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(startDate, endDate, null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m, OtherUnpaidCount: 2, OtherAmount: 25m)]);

        Services.AddSingleton(orderDataServiceMock.Object);

        Services.GetRequiredService<NavigationManager>().NavigateTo(AdminPaymentsSummaryBase.PageUrl(startDate, endDate));
        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
        {
            IElement link = component.Find("a.other-obligations-link");
            Assert.Contains("(2)", link.TextContent);
            Assert.Equal("/admin/payments/obligations?userId=u1&endDate=2026-09-30", link.GetAttribute("href"));

            List<string> cells = component.Find("tbody tr").QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()).ToList();
            Assert.Contains("32", cells[7]);
        });
    }

    [Fact]
    public void AdminPaymentsSummary_WhenEmployeeOwesOnlyOtherObligations_ShouldNotShowPayButton()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 0, 0m, 0m, 0m, OtherUnpaidCount: 1, OtherAmount: 9m)]);

        Services.AddSingleton(orderDataServiceMock.Object);

        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
        {
            Assert.Single(component.FindAll("a.other-obligations-link"));
            Assert.Empty(component.FindAll("button.pay-row-button"));
        });
    }

    [Fact]
    public void PayButton_WhenConfirmed_ShouldPayEmployeeUpToLoadedEndDateAndReload()
    {
        DateTime startDate = new(2026, 9, 1);
        DateTime endDate = new(2026, 9, 30);

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(startDate, endDate, null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m, 1, 4m)]);
        orderDataServiceMock.Setup(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", endDate)))
            .ReturnsAsync(new OrderPayResponseDto(2, 11m));

        Services.AddSingleton(orderDataServiceMock.Object);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);

        Services.GetRequiredService<NavigationManager>().NavigateTo(AdminPaymentsSummaryBase.PageUrl(startDate, endDate));
        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() => component.Find("button.pay-row-button").Click());

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha User: 2 orders marked as paid", component.Find(".alert-success").TextContent);
            orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", endDate)), Times.Once);
            orderDataServiceMock.Verify(service => service.GetOutstandingSummaryAsync(startDate, endDate, null), Times.Exactly(2));
        });
    }

    [Fact]
    public void PayButton_WhenConfirmationIsDeclined_ShouldNotPay()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m)]);

        Services.AddSingleton(orderDataServiceMock.Object);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);

        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() => component.Find("button.pay-row-button").Click());

        orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(It.IsAny<PayAllUnpaidRequestDto>()), Times.Never);
        Assert.Empty(component.FindAll(".alert-success"));
    }

    [Fact]
    public void AdminPaymentsSummary_WhenAllDatesIsInQuery_ShouldLoadAllUnpaidAndPayWithoutDate()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync(null))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 3, 30m, 0m, 30m)]);
        orderDataServiceMock.Setup(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", null)))
            .ReturnsAsync(new OrderPayResponseDto(3, 30m));

        Services.AddSingleton(orderDataServiceMock.Object);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);

        Services.GetRequiredService<NavigationManager>().NavigateTo(AdminPaymentsSummaryBase.AllDatesPageUrl);
        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() =>
            Assert.Contains("Period: All dates", component.Find(".outstanding-period").TextContent));

        component.Find("button.pay-row-button").Click();

        component.WaitForAssertion(() =>
            orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", null)), Times.Once));
        orderDataServiceMock.Verify(
            service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public void PrintButton_WhenClicked_ShouldNavigateToPrintableReportForLoadedPeriod()
    {
        DateTime startDate = new(2026, 9, 1);
        DateTime endDate = new(2026, 9, 30);

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(startDate, endDate))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m)]);

        Services.AddSingleton(orderDataServiceMock.Object);

        Services.GetRequiredService<NavigationManager>().NavigateTo(AdminPaymentsSummaryBase.PageUrl(startDate, endDate));
        IRenderedComponent<AdminPaymentsSummary> component = RenderComponent<AdminPaymentsSummary>();

        component.WaitForAssertion(() => component.Find("button.btn-outline-primary").Click());

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/reports/outstanding?startDate=2026-09-01&endDate=2026-09-30", navigationManager.Uri);
    }
}
