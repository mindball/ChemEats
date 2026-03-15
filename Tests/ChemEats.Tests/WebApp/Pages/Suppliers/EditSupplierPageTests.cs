using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class EditSupplierPageTests : TestContext
{
    [Fact]
    public void EditSupplier_WhenAuthorizedAdmin_ShouldRenderSupplierForm()
    {
        Guid supplierId = Guid.NewGuid();

        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock
            .Setup(service => service.GetSupplierDetailsAsync(supplierId))
            .ReturnsAsync(new SupplierDto
            {
                Id = supplierId,
                Name = "Supplier One",
                VatNumber = "BG123",
                PaymentTerms = PaymentTermsUI.Net10,
                Menus = []
            });

        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock
            .Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1")]);

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(employeeServiceMock.Object);

        IRenderedComponent<EditSupplier> component = RenderComponent<EditSupplier>(
            parameters => parameters.Add(parameter => parameter.SupplierId, supplierId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Edit Supplier", component.Markup);
            Assert.Contains("Update Supplier", component.Markup);
            Assert.Contains("Supplier One", component.Markup);
        });
    }

    [Fact]
    public void EditSupplier_WhenNotAuthorized_ShouldRedirectToLogin()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetNotAuthorized();

        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();

        Services.AddSingleton(supplierServiceMock.Object);
        Services.AddSingleton(employeeServiceMock.Object);

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        RenderComponent<EditSupplier>(parameters => parameters.Add(parameter => parameter.SupplierId, Guid.NewGuid()));

        Assert.Contains("Account/Login", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
