using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using WebApp.Infrastructure.States;
using WebApp.Pages.Menus;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Menus;

public class ViewMenusPageTests : TestContext
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

        Mock<IMenuReportService> reportServiceMock = new();

        Services.AddSingleton(menuDataServiceMock.Object);
        Services.AddSingleton(supplierDataServiceMock.Object);
        Services.AddSingleton(reportServiceMock.Object);

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
}
