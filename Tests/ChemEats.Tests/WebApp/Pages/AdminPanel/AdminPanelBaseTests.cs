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

        public Task RemoveRoleAsyncPublic(string userId, string roleName) => RemoveRoleAsync(userId, roleName);

        public Task SaveAsyncPublic() => SaveAsync();

        public void SetPortionAmount(decimal portionAmount)
        {
            PortionAmount = portionAmount;
        }
    }
}
