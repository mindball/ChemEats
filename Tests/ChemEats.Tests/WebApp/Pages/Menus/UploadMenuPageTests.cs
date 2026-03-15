using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Menus;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Menus;

public class UploadMenuPageTests : TestContext
{
    [Fact]
    public void UploadMenu_WhenAuthorized_ShouldRenderCreateMenuUi()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("supervisor-user");
        authorizationContext.SetRoles("Supervisor");

        Mock<IMenuDataService> menuDataServiceMock = new();
        Mock<ISupplierDataService> supplierDataServiceMock = new();
        supplierDataServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([new SupplierDto { Id = Guid.NewGuid(), Name = "Supplier One", VatNumber = "BG123", PaymentTerms = PaymentTermsUI.Net10, Menus = [] }]);

        Services.AddSingleton(menuDataServiceMock.Object);
        Services.AddSingleton(supplierDataServiceMock.Object);

        IRenderedComponent<UploadMenu> component = RenderComponent<UploadMenu>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Create Menu", component.Markup);
            Assert.Contains("Menu File Upload", component.Markup);
            Assert.Contains("Upload Menu", component.Markup);
        });
    }
}
