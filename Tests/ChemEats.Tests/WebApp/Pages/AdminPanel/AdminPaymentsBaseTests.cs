using Microsoft.JSInterop;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Orders;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Employees;
using WebApp.Services.Orders;
using WebApp.Services.Settings;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminPaymentsBaseTests
{
    private static readonly DateTime StartDate = new(2026, 10, 1);
    private static readonly DateTime EndDate = new(2026, 10, 20);

    private readonly Mock<IOrderDataService> _orderDataServiceMock = new();
    private readonly Mock<IJSRuntime> _jsRuntimeMock = new();

    [Fact]
    public async Task SearchOrdersAsync_WhenEmployeeHasEarlierDebt_ShouldExposeEarlierAndTotalDue()
    {
        SetupOrders([CreateItem(10m)]);
        SetupOutstanding(new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m, 2, 15m));
        AdminPaymentsBaseTestHarness harness = CreateHarness();

        await harness.SearchOrdersAsyncPublic();

        Assert.Equal(2, harness.EarlierUnpaidCountPublic);
        Assert.Equal(15m, harness.EarlierNetAmountPublic);
        Assert.Equal(3, harness.UnpaidUpToEndCountPublic);
        Assert.Equal(22m, harness.TotalDueUpToEndPublic);
        _orderDataServiceMock.Verify(service => service.GetOutstandingSummaryAsync(StartDate, EndDate, "u1"), Times.Once);
    }

    [Fact]
    public async Task SearchOrdersAsync_WhenOrderIsPendingOrCancelled_ShouldNotCountOrSelectIt()
    {
        UserOrderPaymentItemDto unpaid = CreateItem(10m);
        UserOrderPaymentItemDto cancelled = CreateItem(8m, isCancelled: true);
        UserOrderPaymentItemDto pending = CreateItem(6m, isCompleted: false);
        SetupOrders([unpaid, cancelled, pending]);
        SetupOutstanding(null);
        AdminPaymentsBaseTestHarness harness = CreateHarness();

        await harness.SearchOrdersAsyncPublic();
        harness.SelectAllPublic();

        Assert.Equal(1, harness.UnpaidOrdersPublic);
        Assert.Equal(10m, harness.TotalUnpaidPublic);
        Assert.Equal([unpaid.OrderId], harness.SelectedOrderIdsPublic);
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenConfirmed_ShouldPayUpToLoadedEndDateAndKeepSuccessMessage()
    {
        SetupOrders([CreateItem(10m)]);
        SetupOutstanding(new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m, 1, 5m));
        _orderDataServiceMock.Setup(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", EndDate)))
            .ReturnsAsync(new OrderPayResponseDto(2, 12m));
        _jsRuntimeMock.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);
        AdminPaymentsBaseTestHarness harness = CreateHarness();
        await harness.SearchOrdersAsyncPublic();

        await harness.PayAllUnpaidAsyncPublic();

        _orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", EndDate)), Times.Once);
        Assert.StartsWith("Successfully marked 2 orders as paid.", harness.SuccessMessagePublic);
        Assert.Null(harness.ErrorMessagePublic);
        _orderDataServiceMock.Verify(service => service.GetOrdersByUserForPeriodAsync("u1", StartDate, EndDate), Times.Exactly(2));
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenConfirmationIsDeclined_ShouldNotPay()
    {
        SetupOrders([CreateItem(10m)]);
        SetupOutstanding(new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m));
        _jsRuntimeMock.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(false);
        AdminPaymentsBaseTestHarness harness = CreateHarness();
        await harness.SearchOrdersAsyncPublic();

        await harness.PayAllUnpaidAsyncPublic();

        _orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(It.IsAny<PayAllUnpaidRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenServiceFails_ShouldSetErrorMessage()
    {
        SetupOrders([CreateItem(10m)]);
        SetupOutstanding(new EmployeeOutstandingSummaryDto("u1", "Alpha User", 1, 10m, 3m, 7m));
        _orderDataServiceMock.Setup(service => service.PayAllUnpaidAsync(It.IsAny<PayAllUnpaidRequestDto>()))
            .ReturnsAsync((OrderPayResponseDto?)null);
        _jsRuntimeMock.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);
        AdminPaymentsBaseTestHarness harness = CreateHarness();
        await harness.SearchOrdersAsyncPublic();

        await harness.PayAllUnpaidAsyncPublic();

        Assert.Equal("Failed to process payment. Please try again.", harness.ErrorMessagePublic);
        Assert.Null(harness.SuccessMessagePublic);
    }

    [Fact]
    public async Task PortionPreview_WhenTwoOrdersOnSameDayAreSelected_ShouldGoToMostExpensiveOrder()
    {
        UserOrderPaymentItemDto cheap = CreateItem(4m);
        UserOrderPaymentItemDto expensive = CreateItem(9m);
        SetupOrders([cheap, expensive]);
        SetupOutstanding(null);
        AdminPaymentsBaseTestHarness harness = CreateHarness(companyPortion: 5m);
        await harness.InitializeAsyncPublic();
        harness.SetSearch("u1", StartDate, EndDate);
        await harness.SearchOrdersAsyncPublic();

        harness.SelectAllPublic();

        Assert.Equal(5m, harness.PortionMapPublic[expensive.OrderId]);
        Assert.False(harness.PortionMapPublic.ContainsKey(cheap.OrderId));
    }

    [Fact]
    public async Task SearchOrdersAsync_WhenAllDatesIsChecked_ShouldLoadAllUnpaidAndPayWithoutDate()
    {
        UserOrderPaymentItemDto unpaid = CreateItem(10m);
        _orderDataServiceMock.Setup(service => service.GetUnpaidOrdersByUserAsync("u1", null)).ReturnsAsync([unpaid]);
        _orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync("u1"))
            .ReturnsAsync([new EmployeeOutstandingSummaryDto("u1", "Alpha User", 4, 40m, 0m, 40m)]);
        _orderDataServiceMock.Setup(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", null)))
            .ReturnsAsync(new OrderPayResponseDto(4, 40m));
        _jsRuntimeMock.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);
        AdminPaymentsBaseTestHarness harness = CreateHarness();
        harness.SetAllDates(true);

        await harness.SearchOrdersAsyncPublic();
        await harness.PayAllUnpaidAsyncPublic();

        Assert.Equal(4, harness.UnpaidUpToEndCountPublic);
        Assert.Equal(40m, harness.TotalDueUpToEndPublic);
        _orderDataServiceMock.Verify(service => service.PayAllUnpaidAsync(new PayAllUnpaidRequestDto("u1", null)), Times.Once);
        _orderDataServiceMock.Verify(
            service => service.GetOrdersByUserForPeriodAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnInitializedAsync_WhenUserIsInQuery_ShouldOpenThatEmployeesUnpaidOrders()
    {
        _orderDataServiceMock.Setup(service => service.GetUnpaidOrdersByUserAsync("u7", null)).ReturnsAsync([]);
        _orderDataServiceMock.Setup(service => service.GetAllOutstandingAsync("u7")).ReturnsAsync([]);
        AdminPaymentsBaseTestHarness harness = CreateHarness();
        harness.QueryUserId = "u7";
        harness.QueryAllDates = true;

        await harness.InitializeAsyncPublic();

        _orderDataServiceMock.Verify(service => service.GetUnpaidOrdersByUserAsync("u7", null), Times.Once);
        Assert.Equal("No unpaid orders.", harness.SuccessMessagePublic);
    }

    private AdminPaymentsBaseTestHarness CreateHarness(decimal companyPortion = 0m)
    {
        Mock<IEmployeeDataService> employeeDataServiceMock = new();
        employeeDataServiceMock.Setup(service => service.GetAllEmployeesAsync()).ReturnsAsync([]);

        Mock<ISettingsDataService> settingsDataServiceMock = new();
        settingsDataServiceMock.Setup(service => service.GetCompanyPortionAsync()).ReturnsAsync(companyPortion);

        AdminPaymentsBaseTestHarness harness = new(
            _orderDataServiceMock.Object,
            employeeDataServiceMock.Object,
            settingsDataServiceMock.Object,
            _jsRuntimeMock.Object);
        harness.SetSearch("u1", StartDate, EndDate);
        return harness;
    }

    private void SetupOrders(List<UserOrderPaymentItemDto> orders)
    {
        _orderDataServiceMock.Setup(service => service.GetOrdersByUserForPeriodAsync("u1", StartDate, EndDate))
            .ReturnsAsync(orders);
    }

    private void SetupOutstanding(EmployeeOutstandingSummaryDto? summary)
    {
        _orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(StartDate, EndDate, "u1"))
            .ReturnsAsync(summary is null ? [] : [summary]);
    }

    private static UserOrderPaymentItemDto CreateItem(decimal price, bool isCancelled = false, bool isCompleted = true)
    {
        return new UserOrderPaymentItemDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Soup",
            Guid.NewGuid(),
            "Supplier",
            new DateTime(2026, 10, 5, 9, 0, 0),
            new DateTime(2026, 10, 5),
            price,
            PaymentStatusDto.Unpaid,
            null,
            false,
            0m,
            price,
            isCancelled,
            isCompleted && !isCancelled);
    }

    private sealed class AdminPaymentsBaseTestHarness : AdminPaymentsBase
    {
        public AdminPaymentsBaseTestHarness(
            IOrderDataService orderDataService,
            IEmployeeDataService employeeDataService,
            ISettingsDataService settingsDataService,
            IJSRuntime jsRuntime)
        {
            OrderDataService = orderDataService;
            EmployeeDataService = employeeDataService;
            SettingsDataService = settingsDataService;
            JsRuntime = jsRuntime;
        }

        public int EarlierUnpaidCountPublic => EarlierUnpaidCount;
        public decimal EarlierNetAmountPublic => EarlierNetAmount;
        public int UnpaidUpToEndCountPublic => UnpaidUpToEndCount;
        public decimal TotalDueUpToEndPublic => TotalDueUpToEnd;
        public int UnpaidOrdersPublic => UnpaidOrders;
        public decimal TotalUnpaidPublic => TotalUnpaid;
        public HashSet<Guid> SelectedOrderIdsPublic => SelectedOrderIds;
        public Dictionary<Guid, decimal> PortionMapPublic => PortionMap;
        public string? SuccessMessagePublic => SuccessMessage;
        public string? ErrorMessagePublic => ErrorMessage;

        public Task InitializeAsyncPublic() => OnInitializedAsync();
        public Task SearchOrdersAsyncPublic() => SearchOrdersAsync();
        public Task PayAllUnpaidAsyncPublic() => PayAllUnpaidAsync();
        public void SelectAllPublic() => SelectAll();

        public void SetAllDates(bool allDates) => AllDates = allDates;

        public void SetSearch(string userId, DateTime startDate, DateTime endDate)
        {
            SelectedUserId = userId;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
