using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using Moq;
using Shared.DTOs.Orders;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminPaymentsSummaryBaseTests
{
    [Fact]
    public async Task LoadSummaryAsync_WhenServiceReturnsSummaries_ShouldPopulateRowsAndTotals()
    {
        DateTime startDate = new(2026, 10, 1);
        DateTime endDate = new(2026, 10, 31);

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(startDate, endDate))
            .ReturnsAsync([
                new EmployeeOutstandingSummaryDto("u1", "Alpha User", 3, 30m, 6m, 24m),
                new EmployeeOutstandingSummaryDto("u2", "Beta User", 1, 9m, 3m, 6m)
            ]);

        AdminPaymentsSummaryBaseTestHarness harness = new(orderDataServiceMock.Object);
        harness.SetPeriod(startDate, endDate);

        await harness.LoadSummaryAsyncPublic();

        Assert.Equal(2, harness.SummariesPublic!.Count);
        Assert.Equal(4, harness.TotalUnpaidCountPublic);
        Assert.Equal(39m, harness.TotalGrossAmountPublic);
        Assert.Equal(9m, harness.TotalPortionAmountPublic);
        Assert.Equal(30m, harness.TotalNetAmountPublic);
        Assert.Null(harness.ErrorMessagePublic);
        Assert.False(harness.IsLoadingPublic);
    }

    [Fact]
    public async Task LoadSummaryAsync_WhenEndDateIsBeforeStartDate_ShouldSetErrorAndNotCallService()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();

        AdminPaymentsSummaryBaseTestHarness harness = new(orderDataServiceMock.Object);
        harness.SetPeriod(new DateTime(2026, 10, 31), new DateTime(2026, 10, 1));

        await harness.LoadSummaryAsyncPublic();

        Assert.Equal("End date cannot be before start date.", harness.ErrorMessagePublic);
        Assert.Null(harness.SummariesPublic);
        orderDataServiceMock.Verify(
            service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadSummaryAsync_WhenDateIsMissing_ShouldSetError()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();

        AdminPaymentsSummaryBaseTestHarness harness = new(orderDataServiceMock.Object);
        harness.SetPeriod(null, new DateTime(2026, 10, 31));

        await harness.LoadSummaryAsyncPublic();

        Assert.Equal("Please select a start and end date.", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task LoadSummaryAsync_WhenServiceFails_ShouldSetErrorMessage()
    {
        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetOutstandingSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((List<EmployeeOutstandingSummaryDto>?)null);

        AdminPaymentsSummaryBaseTestHarness harness = new(orderDataServiceMock.Object);
        harness.SetPeriod(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        await harness.LoadSummaryAsyncPublic();

        Assert.Equal("Failed to load the outstanding summary. Please try again.", harness.ErrorMessagePublic);
        Assert.Null(harness.SummariesPublic);
        Assert.Equal(0m, harness.TotalNetAmountPublic);
    }

    private sealed class AdminPaymentsSummaryBaseTestHarness : AdminPaymentsSummaryBase
    {
        public AdminPaymentsSummaryBaseTestHarness()
        {
            L = new PassThroughStringLocalizer<SharedResource>();
        }

        public AdminPaymentsSummaryBaseTestHarness(IOrderDataService orderDataService) : this()
        {
            OrderDataService = orderDataService;
        }

        public List<EmployeeOutstandingSummaryDto>? SummariesPublic => Summaries;
        public string? ErrorMessagePublic => ErrorMessage;
        public bool IsLoadingPublic => IsLoading;
        public int TotalUnpaidCountPublic => TotalUnpaidCount;
        public decimal TotalGrossAmountPublic => TotalGrossAmount;
        public decimal TotalPortionAmountPublic => TotalPortionAmount;
        public decimal TotalNetAmountPublic => TotalNetAmount;

        public Task LoadSummaryAsyncPublic() => LoadSummaryAsync();

        public void SetPeriod(DateTime? startDate, DateTime? endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
