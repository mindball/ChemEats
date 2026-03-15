using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class GetSuppliersPageTests : TestContext
{
    [Fact]
    public void GetSuppliers_WhenAuthorizedAdminAndDataExists_ShouldRenderSuppliersTable()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        SupplierDto supplier = new()
        {
            Id = Guid.NewGuid(),
            Name = "Supplier One",
            VatNumber = "BG123",
            PaymentTerms = PaymentTermsUI.Net10,
            Menus = []
        };

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([supplier]);

        Mock<IJSRuntime> jsRuntimeMock = new();

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(jsRuntimeMock.Object);

        IRenderedComponent<GetSuppliers> component = RenderComponent<GetSuppliers>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("All Suppliers (1)", component.Markup);
            Assert.Contains("Supplier One", component.Markup);
        });
    }

    [Fact]
    public void GetSuppliers_WhenAuthorizedAdminAndNoData_ShouldRenderLoadingState()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([]);

        Mock<IJSRuntime> jsRuntimeMock = new();

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(jsRuntimeMock.Object);

        IRenderedComponent<GetSuppliers> component = RenderComponent<GetSuppliers>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Loading suppliers", component.Markup);
        });
    }

    [Fact]
    public void GetSuppliers_WhenNotAuthorized_ShouldRedirectToLogin()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetNotAuthorized();

        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IJSRuntime> jsRuntimeMock = new();

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(jsRuntimeMock.Object);

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        RenderComponent<GetSuppliers>();

        Assert.Contains("Account/Login", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
