using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Obligations;
using WebApp.Pages.Orders;
using WebApp.Services.Obligations;

namespace ChemEats.Tests.WebApp.Pages.Orders;

public class MyObligationsPageTests : TestContext
{
    private readonly Mock<IObligationDataService> _obligationDataServiceMock = new();

    public MyObligationsPageTests()
    {
        Services.AddSingleton(_obligationDataServiceMock.Object);
    }

    [Fact]
    public void MyObligations_WhenObligationsLoaded_ShouldRenderRowsAndUnpaidTotalForCurrentMonth()
    {
        DateTime monthStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

        _obligationDataServiceMock.Setup(service => service.GetMyObligationsAsync(monthStart, monthEnd))
            .ReturnsAsync([
                CreateDto("Printer toner", 12.5m, PaymentStatusDto.Unpaid),
                CreateDto("Keyboard", 30m, PaymentStatusDto.Paid),
                CreateDto("Mouse", 7.5m, PaymentStatusDto.Unpaid)
            ]);

        IRenderedComponent<MyObligations> component = RenderComponent<MyObligations>();

        component.WaitForAssertion(() =>
        {
            IRefreshableElementCollection<IElement> rows = component.FindAll("tbody tr");
            Assert.Equal(3, rows.Count);
            Assert.Contains("Printer toner", rows[0].TextContent);
            Assert.Contains("Unpaid (2)", component.Find("tfoot").TextContent);
            Assert.Equal(20m.ToString("C"), component.Find(".my-obligations-unpaid-total").TextContent.Trim());
        });
        _obligationDataServiceMock.Verify(service => service.GetMyObligationsAsync(monthStart, monthEnd), Times.Once);
    }

    [Fact]
    public void MyObligations_WhenNoObligations_ShouldShowEmptyMessage()
    {
        _obligationDataServiceMock.Setup(service => service.GetMyObligationsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync([]);

        IRenderedComponent<MyObligations> component = RenderComponent<MyObligations>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("No other obligations for this period.", component.Markup);
            Assert.Empty(component.FindAll("table"));
        });
    }

    [Fact]
    public void MyObligations_WhenServiceFails_ShouldShowError()
    {
        _obligationDataServiceMock.Setup(service => service.GetMyObligationsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((List<OtherObligationDto>?)null);

        IRenderedComponent<MyObligations> component = RenderComponent<MyObligations>();

        component.WaitForAssertion(() =>
            Assert.Contains("Failed to load your obligations", component.Find(".alert-danger").TextContent));
    }

    private static OtherObligationDto CreateDto(string description, decimal amount, PaymentStatusDto status) =>
        new(Guid.NewGuid(), "u1", "User One", Guid.NewGuid(), description, amount, DateTime.Today, DateTime.Now, "Admin",
            status, status == PaymentStatusDto.Paid ? DateTime.Today : null, null);
}
