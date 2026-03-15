using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Shared.DTOs.Menus;
using Shared.DTOs.Orders;
using WebApp.Pages.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.Orders;

public class MyOrdersBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_WhenUserIsNotAuthenticated_ShouldSetErrorMessage()
    {
        Mock<IMenuDataService> menuDataServiceMock = new();
        Mock<IOrderDataService> orderDataServiceMock = new();

        AuthenticationState authenticationState = CreateAuthenticationState(false, string.Empty);
        MyOrdersBaseTestHarness harness = new(menuDataServiceMock.Object, orderDataServiceMock.Object, Task.FromResult(authenticationState));

        await harness.OnInitializedAsyncPublic();

        Assert.Equal("You must be signed in to view your orders.", harness.ErrorMessagePublic);
    }

    [Fact]
    public void FormatBulgarianDate_ShouldReturnLocalizedDateString()
    {
        string formatted = MyOrdersBaseTestHarness.FormatBulgarianDatePublic(new DateTime(2026, 01, 10));

        Assert.Contains("10.01.2026", formatted);
    }

    private static AuthenticationState CreateAuthenticationState(bool isAuthenticated, string userName)
    {
        ClaimsIdentity identity = isAuthenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], "TestAuth")
            : new ClaimsIdentity();

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private sealed class MyOrdersBaseTestHarness : MyOrdersBase
    {
        public MyOrdersBaseTestHarness(
            IMenuDataService menuDataService,
            IOrderDataService orderDataService,
            Task<AuthenticationState> authenticationStateTask)
        {
            MenuDataService = menuDataService;
            OrderDataService = orderDataService;
            AuthenticationStateTask = authenticationStateTask;
        }

        public string? ErrorMessagePublic => ErrorMessage;
        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public static string FormatBulgarianDatePublic(DateTime date) => FormatBulgarianDate(date);
    }
}
