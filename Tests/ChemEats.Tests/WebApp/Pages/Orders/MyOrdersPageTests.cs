using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Pages.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.Orders;

public class MyOrdersPageTests : LocalizedTestContext
{
    [Fact]
    public void LoadOrders_WhenSomeOrdersArePaid_ShouldSplitPaidAndRemainingAndExcludeCancelled()
    {
        DateTime tomorrow = DateTime.Today.AddDays(1);
        List<UserOrderItemDto> orders =
        [
            CreateItem("Soup", tomorrow, 8m, "Pending", expectedPortion: 2m),
            CreateItem("Bread", tomorrow, 1m, "Pending"),
            CreateItem("Steak", tomorrow, 20m, "Cancelled"),
            CreateItem("Salad", DateTime.Today, 5m, "Completed", portionApplied: 2m, paidOn: DateTime.Today)
        ];

        IRenderedComponent<MyOrders> component = RenderPage(orders);

        component.Find("button.btn.btn-primary").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Portion (at payment):", component.Markup);
            Assert.Equal($"-{Money(4m)}", component.Find("#total-portion").TextContent.Trim());
            Assert.Equal(Money(10m), component.Find("#total-net").TextContent.Trim());
            Assert.Equal(Money(3m), component.Find("#total-paid").TextContent.Trim());
            Assert.Equal(Money(7m), component.Find("#total-remaining").TextContent.Trim());
            Assert.Contains("Paid (1):", component.Markup);
            Assert.Contains("Remaining to pay (2):", component.Markup);
            Assert.Contains($"Paid {DateTime.Today:dd.MM.yyyy}", component.Markup);
        });
    }

    private static string Money(decimal amount) => amount.ToString("C", CultureInfo.CurrentCulture);

    private IRenderedComponent<MyOrders> RenderPage(List<UserOrderItemDto> orders)
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("employee");

        Mock<IMenuDataService> menuDataServiceMock = new();
        menuDataServiceMock.Setup(service => service.GetAllMenusAsync(true))
            .ReturnsAsync(Array.Empty<MenuDto>());

        Mock<IOrderDataService> orderDataServiceMock = new();
        orderDataServiceMock.Setup(service => service.GetMyOrderItemsAsync(
                It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(orders);

        Services.AddSingleton(menuDataServiceMock.Object);
        Services.AddSingleton(orderDataServiceMock.Object);

        return RenderComponent<MyOrders>();
    }

    private static UserOrderItemDto CreateItem(
        string mealName,
        DateTime menuDate,
        decimal price,
        string status,
        decimal expectedPortion = 0m,
        decimal portionApplied = 0m,
        DateTime? paidOn = null) =>
        new(
            Guid.NewGuid(),
            "user-1",
            Guid.NewGuid(),
            mealName,
            Guid.NewGuid(),
            "Supplier",
            DateTime.Now,
            menuDate,
            price,
            status,
            false,
            portionApplied > 0m,
            portionApplied,
            price - portionApplied,
            IsPaid: paidOn.HasValue,
            PaidOn: paidOn,
            ExpectedPortionAmount: expectedPortion);
}
