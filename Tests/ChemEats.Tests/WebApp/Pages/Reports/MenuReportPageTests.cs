using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Reports;
using WebApp.Pages.Reports;
using WebApp.Services.Menus;

namespace ChemEats.Tests.WebApp.Pages.Reports;

public class MenuReportPageTests : TestContext
{
    private readonly Mock<IMenuReportService> _reportServiceMock = new();

    public MenuReportPageTests()
    {
        Services.AddSingleton(_reportServiceMock.Object);
    }

    [Fact]
    public void MenuReport_WhenReportLoaded_ShouldRenderOrdersAndTotals()
    {
        MenuReportDto report = CreateReport(
            new MenuReportOrderDto("Иван Петров", "Супа топчета", 4.50m),
            new MenuReportOrderDto("John Doe", "Shopska salad", 6m),
            new MenuReportOrderDto("Мария Иванова", "Супа топчета", 4.50m));
        SetupReport(report);

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        Assert.Contains("Menu Report — Доставчик ООД", component.Markup);
        Assert.Contains("Иван Петров", component.Markup);
        Assert.Contains("Мария Иванова", component.Markup);
        Assert.Equal(3, component.FindAll("table.report-table")[0].QuerySelectorAll("tbody tr").Length);
        Assert.Contains(15m.ToString("0.00 €"), component.Find(".report-total-amount").TextContent);
    }

    [Fact]
    public void MenuReport_WhenReportLoaded_ShouldGroupMealsByCountDescending()
    {
        MenuReportDto report = CreateReport(
            new MenuReportOrderDto("A", "Salad", 6m),
            new MenuReportOrderDto("B", "Soup", 4m),
            new MenuReportOrderDto("C", "Soup", 4m));
        SetupReport(report);

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        IElement summaryTable = component.FindAll("table.report-table")[1];
        List<string> rows = summaryTable.QuerySelectorAll("tbody tr")
            .Select(row => string.Join("|", row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())))
            .ToList();

        Assert.Equal(["1|Soup|2 бр.", "2|Salad|1 бр."], rows);
    }

    [Fact]
    public void MenuReport_WhenMenuHasNoOrders_ShouldShowEmptyMessageWithoutTables()
    {
        MenuReportDto report = CreateReport();
        SetupReport(report);

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        Assert.Contains("No orders for this menu.", component.Markup);
        Assert.Empty(component.FindAll("table.report-table"));
        Assert.Contains(0m.ToString("0.00 €"), component.Find(".report-total-amount").TextContent);
    }

    [Fact]
    public void MenuReport_WhenServiceReturnsNull_ShouldShowErrorAndDisablePrint()
    {
        Guid menuId = Guid.NewGuid();
        _reportServiceMock
            .Setup(service => service.GetMenuReportAsync(menuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuReportDto?)null);

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, menuId));

        Assert.Contains("The report could not be loaded", component.Find(".alert-danger").TextContent);
        Assert.True(component.Find("button.btn-primary").HasAttribute("disabled"));
        Assert.Empty(component.FindAll(".report-sheet"));
    }

    [Fact]
    public void MenuReport_WhenServiceThrows_ShouldShowUnexpectedError()
    {
        Guid menuId = Guid.NewGuid();
        _reportServiceMock
            .Setup(service => service.GetMenuReportAsync(menuId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, menuId));

        Assert.Contains("Unexpected error while loading the report.", component.Find(".alert-danger").TextContent);
    }

    [Fact]
    public void PrintButton_WhenClicked_ShouldInvokeBrowserPrint()
    {
        MenuReportDto report = CreateReport(new MenuReportOrderDto("A", "Soup", 4m));
        SetupReport(report);
        JSInterop.SetupVoid("print");

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        component.Find("button.btn-primary").Click();

        JSInterop.VerifyInvoke("print");
    }

    [Fact]
    public void BackButton_WhenClicked_ShouldNavigateToMenusPage()
    {
        MenuReportDto report = CreateReport();
        SetupReport(report);

        IRenderedComponent<MenuReport> component = RenderComponent<MenuReport>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        component.Find("button.btn-outline-secondary").Click();

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();
        Assert.Equal("http://localhost/menus/view", navigationManager.Uri);
    }

    [Fact]
    public void DocumentTitle_WhenReportLoaded_ShouldContainSupplierAndMenuDate()
    {
        MenuReportDto report = CreateReport();
        SetupReport(report);

        IRenderedComponent<MenuReportTitleProbe> component = RenderComponent<MenuReportTitleProbe>(
            parameters => parameters.Add(page => page.MenuId, report.MenuId));

        Assert.Equal($"menu-report-Доставчик ООД-{report.MenuDate:yyyyMMdd}", component.Instance.Title);
    }

    private void SetupReport(MenuReportDto report)
    {
        _reportServiceMock
            .Setup(service => service.GetMenuReportAsync(report.MenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
    }

    private static MenuReportDto CreateReport(params MenuReportOrderDto[] orders)
    {
        return new MenuReportDto(
            Guid.NewGuid(),
            "Доставчик ООД",
            new DateTime(2026, 10, 26),
            new DateTime(2026, 10, 25, 12, 0, 0),
            orders);
    }

    public sealed class MenuReportTitleProbe : MenuReportBase
    {
        public string Title => DocumentTitle;
    }
}
