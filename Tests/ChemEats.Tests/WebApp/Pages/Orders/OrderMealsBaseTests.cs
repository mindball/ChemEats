using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using WebApp.Pages.Orders;
using WebApp.Services.Menus;
using WebApp.Services.Orders;

namespace ChemEats.Tests.WebApp.Pages.Orders;

public class OrderMealsBaseTests
{
    [Fact]
    public void UpdateQuantity_ShouldAddAndRemoveSelection()
    {
        Mock<IMenuDataService> menuDataServiceMock = new();
        Mock<IOrderDataService> orderDataServiceMock = new();
        AuthenticationState authenticationState = CreateAuthenticationState(isAuthenticated: true, "user1");
        OrderMealsBaseTestHarness harness = new(menuDataServiceMock.Object, orderDataServiceMock.Object, Task.FromResult(authenticationState));

        (Guid MealId, DateTime Date) key = (Guid.NewGuid(), DateTime.Today.AddDays(1));

        harness.UpdateQuantityPublic(key, "3");
        Assert.Equal(3, harness.GetQuantityPublic(key));

        harness.UpdateQuantityPublic(key, "0");
        Assert.False(harness.IsSelectedPublic(key));
    }

    private static AuthenticationState CreateAuthenticationState(bool isAuthenticated, string userName)
    {
        ClaimsIdentity identity = isAuthenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], "TestAuth")
            : new ClaimsIdentity();

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private sealed class OrderMealsBaseTestHarness : OrderMealsBase
    {
        public OrderMealsBaseTestHarness(
            IMenuDataService menuDataService,
            IOrderDataService orderDataService,
            Task<AuthenticationState> authenticationStateTask)
        {
            MenuDataService = menuDataService;
            OrderDataService = orderDataService;
            AuthenticationStateTask = authenticationStateTask;
        }

        public int GetQuantityPublic((Guid MealId, DateTime Date) key) => GetQuantity(key);

        public bool IsSelectedPublic((Guid MealId, DateTime Date) key) => IsSelected(key);

        public void SetSelection((Guid MealId, DateTime Date) key, int quantity)
        {
            Selected[key] = quantity;
        }

        public void UpdateQuantityPublic((Guid MealId, DateTime Date) key, string value) => UpdateQuantity(key, value);
    }
}
