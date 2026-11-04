using Microsoft.JSInterop;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Obligations;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Employees;
using WebApp.Services.Obligations;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminObligationsBaseTests
{
    private static readonly DateTime StartDate = new(2026, 10, 1);
    private static readonly DateTime EndDate = new(2026, 10, 31);

    [Fact]
    public async Task SearchAsync_WhenServiceReturnsObligations_ShouldPopulateListAndTotals()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([
                CreateDto(10m, PaymentStatusDto.Unpaid),
                CreateDto(5.5m, PaymentStatusDto.Unpaid),
                CreateDto(20m, PaymentStatusDto.Paid)
            ]);

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);

        await harness.SearchAsyncPublic();

        Assert.Equal(3, harness.ObligationsPublic!.Count);
        Assert.Equal(2, harness.UnpaidCountPublic);
        Assert.Equal(15.5m, harness.UnpaidTotalPublic);
        Assert.Equal(20m, harness.PaidTotalPublic);
        Assert.Null(harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task SearchAsync_WhenNoEmployeeIsSelected_ShouldSetErrorAndNotCallService()
    {
        Mock<IObligationDataService> obligationServiceMock = new();

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select(null, StartDate, EndDate);

        await harness.SearchAsyncPublic();

        Assert.Equal("Please select an employee.", harness.ErrorMessagePublic);
        obligationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAsync_WhenServiceFails_ShouldSetErrorMessage()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync((List<OtherObligationDto>?)null);

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);

        await harness.SearchAsyncPublic();

        Assert.Equal("Failed to load the obligations. Please try again.", harness.ErrorMessagePublic);
        Assert.Null(harness.ObligationsPublic);
    }

    [Fact]
    public async Task SaveAsync_WhenAdding_ShouldCreateForLoadedEmployeeAndReload()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([]);
        obligationServiceMock.Setup(service => service.CreateObligationAsync(
                new CreateOtherObligationDto("u1", "Printer toner", 12.5m, new DateTime(2026, 10, 5))))
            .ReturnsAsync(CreateDto(12.5m, PaymentStatusDto.Unpaid));

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        harness.SetForm("  Printer toner ", 12.5m, new DateTime(2026, 10, 5));
        await harness.SaveAsyncPublic();

        Assert.Null(harness.ErrorMessagePublic);
        Assert.Contains("added", harness.SuccessMessagePublic);
        obligationServiceMock.VerifyAll();
        obligationServiceMock.Verify(service => service.GetUserObligationsAsync("u1", StartDate, EndDate), Times.Exactly(2));
    }

    [Theory]
    [InlineData("", 10, "Description is required.")]
    [InlineData("Toner", 0, "Amount must be greater than zero.")]
    public async Task SaveAsync_WhenFormIsInvalid_ShouldSetErrorAndNotCallService(string description, int amount, string expectedError)
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([]);

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        harness.SetForm(description, amount, DateTime.Today);
        await harness.SaveAsyncPublic();

        Assert.Equal(expectedError, harness.ErrorMessagePublic);
        obligationServiceMock.Verify(service => service.CreateObligationAsync(It.IsAny<CreateOtherObligationDto>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenEditing_ShouldUpdateTheEditedObligation()
    {
        OtherObligationDto existing = CreateDto(10m, PaymentStatusDto.Unpaid);

        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([existing]);
        obligationServiceMock.Setup(service => service.UpdateObligationAsync(
                existing.Id,
                new UpdateOtherObligationDto("Keyboard", 30m, existing.Date)))
            .ReturnsAsync(existing with { Description = "Keyboard", Amount = 30m });

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        harness.StartEditPublic(existing);
        harness.SetForm("Keyboard", 30m, existing.Date);
        await harness.SaveAsyncPublic();

        Assert.Contains("updated", harness.SuccessMessagePublic);
        Assert.Null(harness.EditingIdPublic);
        obligationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task SaveAsync_WhenServiceRejects_ShouldSetErrorMessage()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([]);
        obligationServiceMock.Setup(service => service.CreateObligationAsync(It.IsAny<CreateOtherObligationDto>()))
            .ReturnsAsync((OtherObligationDto?)null);

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        harness.SetForm("Toner", 10m, DateTime.Today);
        await harness.SaveAsyncPublic();

        Assert.StartsWith("Failed to save the obligation.", harness.ErrorMessagePublic);
    }

    [Fact]
    public void StartEdit_WhenObligationIsPaid_ShouldNotEnterEditMode()
    {
        AdminObligationsBaseTestHarness harness = new(new Mock<IObligationDataService>().Object);

        harness.StartEditPublic(CreateDto(10m, PaymentStatusDto.Paid));

        Assert.Null(harness.EditingIdPublic);
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenConfirmed_ShouldPayUpToLoadedEndDate()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([CreateDto(10m, PaymentStatusDto.Unpaid)]);
        obligationServiceMock.Setup(service => service.PayAllUnpaidAsync(new ObligationPayAllRequestDto("u1", EndDate)))
            .ReturnsAsync(new ObligationPayResponseDto(1, 10m));

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object, confirm: true);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        await harness.PayAllUnpaidAsyncPublic();

        Assert.Contains("1 obligations marked as paid", harness.SuccessMessagePublic);
        obligationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenConfirmationIsDeclined_ShouldNotPay()
    {
        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([CreateDto(10m, PaymentStatusDto.Unpaid)]);

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object, confirm: false);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        await harness.PayAllUnpaidAsyncPublic();

        obligationServiceMock.Verify(service => service.PayAllUnpaidAsync(It.IsAny<ObligationPayAllRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task PaySelectedAsync_ShouldPaySelectedIdsForLoadedEmployee()
    {
        OtherObligationDto first = CreateDto(10m, PaymentStatusDto.Unpaid);

        Mock<IObligationDataService> obligationServiceMock = new();
        obligationServiceMock.Setup(service => service.GetUserObligationsAsync("u1", StartDate, EndDate))
            .ReturnsAsync([first, CreateDto(5m, PaymentStatusDto.Unpaid)]);
        obligationServiceMock.Setup(service => service.PayObligationsAsync(
                It.Is<ObligationPayRequestDto>(request => request.UserId == "u1" && request.ObligationIds.Single() == first.Id)))
            .ReturnsAsync(new ObligationPayResponseDto(1, 10m));

        AdminObligationsBaseTestHarness harness = new(obligationServiceMock.Object);
        harness.Select("u1", StartDate, EndDate);
        await harness.SearchAsyncPublic();

        harness.ToggleSelectionPublic(first.Id, true);
        Assert.Equal(10m, harness.SelectedTotalPublic);

        await harness.PaySelectedAsyncPublic();

        obligationServiceMock.VerifyAll();
    }

    [Fact]
    public void EmployeeUnpaidPageUrl_ShouldBuildPeriodOrAllDatesLink()
    {
        Assert.Equal("/admin/payments/obligations?userId=u%201&endDate=2026-10-31", AdminObligationsBase.EmployeeUnpaidPageUrl("u 1", EndDate));
        Assert.Equal("/admin/payments/obligations?userId=u1&allDates=true", AdminObligationsBase.EmployeeUnpaidPageUrl("u1", null));
    }

    private static OtherObligationDto CreateDto(decimal amount, PaymentStatusDto status) =>
        new(Guid.NewGuid(), "u1", "User One", "Printer toner", amount, new DateTime(2026, 10, 5), DateTime.Now, "Admin",
            status, status == PaymentStatusDto.Paid ? DateTime.Now : null, null);

    private sealed class AdminObligationsBaseTestHarness : AdminObligationsBase
    {
        public AdminObligationsBaseTestHarness(IObligationDataService obligationDataService, bool confirm = true)
        {
            ObligationDataService = obligationDataService;
            EmployeeDataService = new Mock<IEmployeeDataService>().Object;

            Mock<IJSRuntime> jsRuntimeMock = new();
            jsRuntimeMock.Setup(runtime => runtime.InvokeAsync<bool>("confirm", It.IsAny<object?[]?>()))
                .ReturnsAsync(confirm);
            JsRuntime = jsRuntimeMock.Object;
        }

        public List<OtherObligationDto>? ObligationsPublic => Obligations;
        public string? ErrorMessagePublic => ErrorMessage;
        public string? SuccessMessagePublic => SuccessMessage;
        public int UnpaidCountPublic => UnpaidCount;
        public decimal UnpaidTotalPublic => UnpaidTotal;
        public decimal PaidTotalPublic => PaidTotal;
        public decimal SelectedTotalPublic => SelectedTotal;
        public Guid? EditingIdPublic => EditingId;

        public Task SearchAsyncPublic() => SearchAsync();
        public Task SaveAsyncPublic() => SaveAsync();
        public Task PayAllUnpaidAsyncPublic() => PayAllUnpaidAsync();
        public Task PaySelectedAsyncPublic() => PaySelectedAsync();
        public void StartEditPublic(OtherObligationDto obligation) => StartEdit(obligation);
        public void ToggleSelectionPublic(Guid id, bool isSelected) => ToggleSelection(id, isSelected);

        public void Select(string? userId, DateTime? startDate, DateTime? endDate)
        {
            SelectedUserId = userId;
            StartDate = startDate;
            EndDate = endDate;
        }

        public void SetForm(string? description, decimal? amount, DateTime? date)
        {
            FormDescription = description;
            FormAmount = amount;
            FormDate = date;
        }
    }
}
