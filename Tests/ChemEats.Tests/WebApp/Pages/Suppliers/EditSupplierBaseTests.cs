using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class EditSupplierBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_ShouldLoadUsersAndSupplier()
    {
        Guid supplierId = Guid.NewGuid();

        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1")]);

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.GetSupplierDetailsAsync(supplierId))
            .ReturnsAsync(new SupplierDto
            {
                Id = supplierId,
                Name = "Supplier One",
                VatNumber = "BG123",
                PaymentTerms = PaymentTermsUI.Net10,
                Menus = []
            });

        EditSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object, supplierId);

        await harness.OnInitializedAsyncPublic();

        Assert.Single(harness.AvailableUsersPublic);
        Assert.Equal("Supplier One", harness.SupplierPublic.Name);
        Assert.Equal("BG123", harness.SupplierPublic.VatNumber);
    }

    [Fact]
    public async Task OnInitializedAsync_WhenServiceThrows_ShouldSetErrorMessage()
    {
        Guid supplierId = Guid.NewGuid();

        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock.Setup(service => service.GetAllEmployeesAsync())
            .ThrowsAsync(new InvalidOperationException("users failed"));

        Mock<ISupplierDataService> supplierServiceMock = new();

        EditSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object, supplierId);

        await harness.OnInitializedAsyncPublic();

        Assert.Contains("Error loading supplier", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WhenUpdateReturnsNull_ShouldSetErrorMessage()
    {
        Mock<IEmployeeDataService> employeeServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.UpdateSupplierAsync(It.IsAny<UpdateSupplierDto>()))
            .ReturnsAsync((UpdateSupplierDto?)null);

        EditSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object, Guid.NewGuid());

        await harness.UpdateSupplierAsyncPublic();

        Assert.Equal("Failed to update supplier.", harness.ErrorMessagePublic);
        Assert.False(harness.IsSubmittingPublic);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WhenUpdateSucceeds_ShouldSetSuccessMessage()
    {
        UpdateSupplierDto updated = new()
        {
            Id = Guid.NewGuid(),
            Name = "Updated Supplier",
            VatNumber = "BG123"
        };

        Mock<IEmployeeDataService> employeeServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.UpdateSupplierAsync(It.IsAny<UpdateSupplierDto>()))
            .ReturnsAsync(updated);

        EditSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object, updated.Id);

        await harness.UpdateSupplierAsyncPublic();

        Assert.Contains("updated successfully", harness.SuccessMessagePublic);
        Assert.Equal("Updated Supplier", harness.SupplierPublic.Name);
    }

    private static EditSupplierBaseTestHarness CreateHarness(ISupplierDataService supplierService, IEmployeeDataService employeeService, Guid supplierId)
    {
        EditSupplierBaseTestHarness harness = new();
        harness.SetDependencies(supplierService, employeeService);
        harness.SupplierId = supplierId;

        return harness;
    }

    private sealed class EditSupplierBaseTestHarness : EditSupplierBase
    {
        public List<EmployeeDto> AvailableUsersPublic => AvailableUsers;
        public string? ErrorMessagePublic => ErrorMessage;
        public bool IsSubmittingPublic => IsSubmitting;
        public UpdateSupplierDto SupplierPublic => Supplier;
        public string? SuccessMessagePublic => SuccessMessage;

        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public Task UpdateSupplierAsyncPublic() => UpdateSupplierAsync();

        public void SetDependencies(ISupplierDataService supplierService, IEmployeeDataService employeeService)
        {
            SupplierService = supplierService;
            EmployeeService = employeeService;
        }
    }
}
