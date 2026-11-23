using Moq;
using Shared.DTOs.Employees;
using Shared.DTOs.Orders;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminPanelBaseTests
{
    [Fact]
    public async Task SaveAsync_WhenPortionIsNegative_ShouldSetValidationError()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        Mock<ISettingsDataService> settingsDataServiceMock = new();
        Mock<IEmployeeDataService> employeeDataServiceMock = new();

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, settingsDataServiceMock.Object, employeeDataServiceMock.Object);
        harness.SetPortionAmount(-1m);

        await harness.SaveAsyncPublic();

        Assert.Equal("Portion amount must be >= 0.", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task SaveAsync_WhenServerAccepts_ShouldSetSuccessMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        Mock<ISettingsDataService> settingsDataServiceMock = new();
        settingsDataServiceMock.Setup(service => service.SetCompanyPortionAsync(3.5m)).ReturnsAsync(true);

        Mock<IEmployeeDataService> employeeDataServiceMock = new();

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, settingsDataServiceMock.Object, employeeDataServiceMock.Object);
        harness.SetPortionAmount(3.5m);

        await harness.SaveAsyncPublic();

        Assert.Contains("Saved portion amount", harness.SuccessMessagePublic);
        Assert.Null(harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task LoadUsersAsync_ShouldPopulateAllUsers()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        Mock<ISettingsDataService> settingsDataServiceMock = new();
        Mock<IEmployeeDataService> employeeDataServiceMock = new();
        employeeDataServiceMock.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1")]);

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, settingsDataServiceMock.Object, employeeDataServiceMock.Object);

        await harness.LoadUsersAsyncPublic();

        Assert.Single(harness.AllUsersPublic!);
        Assert.False(harness.IsLoadingUsersPublic);
    }

    [Fact]
    public async Task AssignRoleAsync_WhenServiceReturnsTrue_ShouldSetSuccessMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        Mock<ISettingsDataService> settingsDataServiceMock = new();
        Mock<IEmployeeDataService> employeeDataServiceMock = new();

        employeeDataServiceMock.Setup(service => service.AssignRoleAsync("u1", "Supervisor")).ReturnsAsync(true);
        employeeDataServiceMock.Setup(service => service.GetAllEmployeesAsync()).ReturnsAsync([]);

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, settingsDataServiceMock.Object, employeeDataServiceMock.Object);

        await harness.AssignRoleAsyncPublic("u1", "Supervisor");

        Assert.Contains("assigned successfully", harness.UserManagementSuccessPublic);
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenServiceReturnsFalse_ShouldSetErrorMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        Mock<ISettingsDataService> settingsDataServiceMock = new();
        Mock<IEmployeeDataService> employeeDataServiceMock = new();

        employeeDataServiceMock.Setup(service => service.RemoveRoleAsync("u1", "Admin")).ReturnsAsync(false);

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, settingsDataServiceMock.Object, employeeDataServiceMock.Object);

        await harness.RemoveRoleAsyncPublic("u1", "Admin");

        Assert.Equal("Failed to remove role.", harness.UserManagementErrorPublic);
    }

    [Fact]
    public async Task LoadPaymentsSummaryAsync_ShouldListAllEmployeesSortedByTotalDue()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync(null))
            .ReturnsAsync([
                new EmployeeOutstandingSummaryDto("u1", "Alpha User", 2, 20m, 6m, 14m),
                new EmployeeOutstandingSummaryDto("u2", "Beta User", 3, 30m, 3m, 27m, 1, 5m)
            ]);

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, Mock.Of<ISettingsDataService>(), Mock.Of<IEmployeeDataService>());

        await harness.LoadPaymentsSummaryAsyncPublic();

        Assert.Equal(["u2", "u1"], harness.OutstandingEmployeesPublic!.Select(s => s.UserId));
        Assert.Equal(6, harness.OutstandingOrderCountPublic);
        Assert.Equal(46m, harness.OutstandingTotalDuePublic);
        Assert.Null(harness.ErrorMessagePublic);
        orderDataServiceMock.Verify(service => service.GetMyPaymentsSummaryAsync(), Times.Never);
    }

    [Fact]
    public async Task LoadPaymentsSummaryAsync_WhenServiceFails_ShouldSetErrorMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync(null))
            .ReturnsAsync((List<EmployeeOutstandingSummaryDto>?)null);

        AdminPanelBaseTestHarness harness = new(orderDataServiceMock.Object, Mock.Of<ISettingsDataService>(), Mock.Of<IEmployeeDataService>());

        await harness.LoadPaymentsSummaryAsyncPublic();

        Assert.Equal("Failed to load outstanding payments.", harness.ErrorMessagePublic);
        Assert.Null(harness.OutstandingEmployeesPublic);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenServerAccepts_ShouldSetSuccessAndReloadUsers()
    {
        Mock<IEmployeeDataService> employeeDataServiceMock = new();
        employeeDataServiceMock
            .Setup(service => service.CreateEmployeeAsync(new CreateEmployeeRequestDto("MM", "Mincho", null)))
            .ReturnsAsync((new TemporaryPasswordDto("u1", "MM", "Temp1234x"), (string?)null));
        employeeDataServiceMock.Setup(service => service.GetAllEmployeesAsync()).ReturnsAsync([]);

        AdminPanelBaseTestHarness harness = new(new Mock<IOrderDataService>().Object, new Mock<ISettingsDataService>().Object, employeeDataServiceMock.Object);
        harness.SetNewEmployee(" mm ", " Mincho ", " ");

        await harness.CreateEmployeeAsyncPublic();

        Assert.NotNull(harness.UserManagementSuccessPublic);
        Assert.Contains("Temp1234x", harness.UserManagementSuccessPublic);
        Assert.Null(harness.UserManagementErrorPublic);
        employeeDataServiceMock.Verify(service => service.GetAllEmployeesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenServerRejects_ShouldShowServerError()
    {
        Mock<IEmployeeDataService> employeeDataServiceMock = new();
        employeeDataServiceMock
            .Setup(service => service.CreateEmployeeAsync(It.IsAny<CreateEmployeeRequestDto>()))
            .ReturnsAsync(((TemporaryPasswordDto?)null, "Abbreviation already exists."));

        AdminPanelBaseTestHarness harness = new(new Mock<IOrderDataService>().Object, new Mock<ISettingsDataService>().Object, employeeDataServiceMock.Object);
        harness.SetNewEmployee("MM", "Mincho", null);

        await harness.CreateEmployeeAsyncPublic();

        Assert.Equal("Abbreviation already exists.", harness.UserManagementErrorPublic);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenFieldsAreMissing_ShouldNotCallServer()
    {
        Mock<IEmployeeDataService> employeeDataServiceMock = new();

        AdminPanelBaseTestHarness harness = new(new Mock<IOrderDataService>().Object, new Mock<ISettingsDataService>().Object, employeeDataServiceMock.Object);
        harness.SetNewEmployee("MM", " ", null);

        await harness.CreateEmployeeAsyncPublic();

        Assert.NotNull(harness.UserManagementErrorPublic);
        employeeDataServiceMock.Verify(service => service.CreateEmployeeAsync(It.IsAny<CreateEmployeeRequestDto>()), Times.Never);
    }

    private sealed class AdminPanelBaseTestHarness : AdminPanelBase
    {
        public AdminPanelBaseTestHarness(
            IOrderDataService orderDataService,
            ISettingsDataService settingsDataService,
            IEmployeeDataService employeeDataService)
        {
            OrderDataService = orderDataService;
            SettingsDataService = settingsDataService;
            EmployeeDataService = employeeDataService;
        }

        public List<EmployeeDto>? AllUsersPublic => AllUsers;
        public string? ErrorMessagePublic => ErrorMessage;
        public bool IsLoadingUsersPublic => IsLoadingUsers;
        public string? SuccessMessagePublic => SuccessMessage;
        public string? UserManagementErrorPublic => UserManagementError;
        public string? UserManagementSuccessPublic => UserManagementSuccess;

        public Task AssignRoleAsyncPublic(string userId, string roleName) => AssignRoleAsync(userId, roleName);

        public Task LoadUsersAsyncPublic() => LoadUsersAsync();

        public Task LoadPaymentsSummaryAsyncPublic() => LoadPaymentsSummaryAsync();
        public List<EmployeeOutstandingSummaryDto>? OutstandingEmployeesPublic => OutstandingEmployees;
        public int OutstandingOrderCountPublic => OutstandingOrderCount;
        public decimal OutstandingTotalDuePublic => OutstandingTotalDue;

        public Task RemoveRoleAsyncPublic(string userId, string roleName) => RemoveRoleAsync(userId, roleName);

        public Task SaveAsyncPublic() => SaveAsync();

        public Task CreateEmployeeAsyncPublic() => CreateEmployeeAsync();

        public void SetNewEmployee(string? abbreviation, string? fullName, string? email)
        {
            NewEmployeeAbbreviation = abbreviation;
            NewEmployeeFullName = fullName;
            NewEmployeeEmail = email;
        }

        public void SetPortionAmount(decimal portionAmount)
        {
            PortionAmount = portionAmount;
        }
    }
}
