using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using System.Text;
using System.Text.Json;
using WebApp.Infrastructure.States;
using WebApp.Pages.Menus;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Menus;

public class ViewMenusPageTests : LocalizedTestContext
{
    [Fact]
    public void ViewMenus_WhenAuthorizedAndSearched_ShouldRenderMenusTable()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        JSInterop.Mode = JSRuntimeMode.Loose;

        Mock<IMenuDataService> menuDataServiceMock = new();
        menuDataServiceMock
            .Setup(service => service.GetAllMenusAsync(false))
            .ReturnsAsync([
                new MenuDto(Guid.NewGuid(), Guid.NewGuid(), "Supplier One", DateTime.Today.AddDays(1), DateTime.Today.AddDays(1).AddHours(12), true, false, [])
            ]);

        Mock<ISupplierDataService> supplierDataServiceMock = new();
        supplierDataServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([new SupplierDto { Id = Guid.NewGuid(), Name = "Supplier One", VatNumber = "BG123", PaymentTerms = PaymentTermsUI.Net10, Menus = [] }]);

        Services.AddSingleton(menuDataServiceMock.Object);
        Services.AddSingleton(supplierDataServiceMock.Object);

        HttpClient httpClient = new() { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(httpClient);
        Services.AddScoped<SessionStorageService>();
        Services.AddScoped<CustomAuthStateProvider>();

        IRenderedComponent<ViewMenus> component = RenderComponent<ViewMenus>();

        component.Find("button.btn.btn-primary").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Found Menus", component.Markup);
            Assert.Contains("Supplier One", component.Markup);
        });
    }

    [Fact]
    public void ReportButton_WhenClicked_ShouldNavigateToPrintableReportPage()
    {
        Guid menuId = Guid.NewGuid();

        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string>("sessionStorage.getItem", "authToken")
            .SetResult(JsonSerializer.Serialize(CreateJwtTokenWithRole("Admin")));

        Mock<IMenuDataService> menuDataServiceMock = new();
        menuDataServiceMock
            .Setup(service => service.GetAllMenusAsync(false))
            .ReturnsAsync([
                new MenuDto(menuId, Guid.NewGuid(), "Supplier One", DateTime.Today.AddDays(1), DateTime.Today.AddDays(1).AddHours(12), true, false, [])
            ]);

        Mock<ISupplierDataService> supplierDataServiceMock = new();
        supplierDataServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([new SupplierDto { Id = Guid.NewGuid(), Name = "Supplier One", VatNumber = "BG123", PaymentTerms = PaymentTermsUI.Net10, Menus = [] }]);

        Services.AddSingleton(menuDataServiceMock.Object);
        Services.AddSingleton(supplierDataServiceMock.Object);

        HttpClient httpClient = new() { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(httpClient);
        Services.AddScoped<SessionStorageService>();
        Services.AddScoped<CustomAuthStateProvider>();

        IRenderedComponent<ViewMenus> component = RenderComponent<ViewMenus>();

        component.Find("button.btn.btn-primary").Click();

        component.WaitForAssertion(() => Assert.NotEmpty(component.FindAll("button.report-button")));

        component.Find("button.report-button").Click();

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();
        Assert.Equal($"http://localhost/reports/menu/{menuId}", navigationManager.Uri);
    }

    private static string CreateJwtTokenWithRole(string role)
    {
        string header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode($"{{\"sub\":\"user-1\",\"name\":\"user\",\"role\":\"{role}\"}}");

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        string base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
