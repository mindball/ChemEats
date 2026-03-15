using ChemEats.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Employees;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class RegisterSupplierBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_ShouldLoadAvailableUsers()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1")]);

        RegisterSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object);

        await harness.OnInitializedAsyncPublic();

        Assert.Single(harness.AvailableUsersPublic);
        Assert.Equal("u1", harness.AvailableUsersPublic[0].UserId);
    }

    [Fact]
    public async Task OnInitializedAsync_WhenEmployeeServiceThrows_ShouldSetErrorMessage()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();
        employeeServiceMock.Setup(service => service.GetAllEmployeesAsync())
            .ThrowsAsync(new InvalidOperationException("load failed"));

        RegisterSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object);

        await harness.OnInitializedAsyncPublic();

        Assert.Contains("Error loading users", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task RegisterSupplierAsync_WhenServiceReturnsNull_ShouldSetErrorMessage()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.AddSupplierAsync(It.IsAny<CreateSupplierDto>()))
            .ReturnsAsync((CreateSupplierDto?)null);

        Mock<IEmployeeDataService> employeeServiceMock = new();
        RegisterSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object);

        await harness.RegisterSupplierAsyncPublic();

        Assert.Equal("Failed to register supplier.", harness.ErrorMessagePublic);
        Assert.False(harness.IsSubmittingPublic);
    }

    [Fact]
    public void ResetForm_ShouldClearMessagesAndSupplierIdentityFields()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IEmployeeDataService> employeeServiceMock = new();
        RegisterSupplierBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, employeeServiceMock.Object);

        harness.SupplierPublic.Name = "Before";
        harness.SupplierPublic.VatNumber = "BG123";
        harness.SetMessages("ok", "error");

        harness.ResetFormPublic();

        Assert.Equal(string.Empty, harness.SupplierPublic.Name);
        Assert.Equal(string.Empty, harness.SupplierPublic.VatNumber);
        Assert.Null(harness.ErrorMessagePublic);
        Assert.Null(harness.SuccessMessagePublic);
    }

    private static RegisterSupplierBaseTestHarness CreateHarness(ISupplierDataService supplierService, IEmployeeDataService employeeService)
    {
        TestNavigationManager navigationManager = new();

        RegisterSupplierBaseTestHarness harness = new();
        harness.SetDependencies(supplierService, employeeService, navigationManager);

        return harness;
    }

    private sealed class RegisterSupplierBaseTestHarness : RegisterSupplierBase
    {
        public List<EmployeeDto> AvailableUsersPublic => AvailableUsers;
        public string? ErrorMessagePublic => ErrorMessage;
        public bool IsSubmittingPublic => IsSubmitting;
        public CreateSupplierDto SupplierPublic => supplier;
        public string? SuccessMessagePublic => SuccessMessage;

        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public Task RegisterSupplierAsyncPublic() => RegisterSupplierAsync();

        public void ResetFormPublic() => ResetForm();

        public void SetMessages(string success, string error)
        {
            SuccessMessage = success;
            ErrorMessage = error;
        }

        public void SetDependencies(
            ISupplierDataService supplierService,
            IEmployeeDataService employeeService,
            NavigationManager navigationManager)
        {
            SupplierService = supplierService;
            EmployeeService = employeeService;
            Navigation = navigationManager;
        }
    }
}
