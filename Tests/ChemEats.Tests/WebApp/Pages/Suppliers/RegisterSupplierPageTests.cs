using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class RegisterSupplierPageTests : TestContext
{
    [Fact]
    public void RegisterSupplier_WhenAuthorizedAdmin_ShouldRenderFormSections()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock
            .Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1")]);

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(employeeServiceMock.Object);

        IRenderedComponent<RegisterSupplier> component = RenderComponent<RegisterSupplier>();

        Assert.Contains("Add Supplier", component.Markup);
        Assert.Contains("Basic Information", component.Markup);
        Assert.Contains("Contact Information", component.Markup);
        Assert.Contains("Address Information", component.Markup);
        Assert.Contains("Register Supplier", component.Markup);
    }

    [Fact]
    public void RegisterSupplier_WhenNotAuthorized_ShouldRedirectToLogin()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetNotAuthorized();

        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(employeeServiceMock.Object);

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        RenderComponent<RegisterSupplier>();

        Assert.Contains("Account/Login", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
