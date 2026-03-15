using System.Reflection;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Orders;
using WebApi.Routes.Orders;

namespace ChemEats.Tests.WebApi.Routes.Orders;

public class OrdersEndpointsTests
{
    [Fact]
    public async Task PlaceOrdersAsync_WhenRequestIsEmpty_ShouldReturnBadRequest()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<IMealRepository> mealRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "PlaceOrdersAsync",
            null,
            orderRepositoryMock.Object,
            mealRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task PlaceOrdersAsync_WhenUserIsUnauthorized_ShouldReturnUnauthorized()
    {
        PlaceOrdersRequestDto request = new([
            new OrderRequestItemDto(Guid.NewGuid(), DateTime.UtcNow, 1)
        ]);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<IMealRepository> mealRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "PlaceOrdersAsync",
            request,
            orderRepositoryMock.Object,
            mealRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderMissing_ShouldReturnNotFound()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MealOrder?)null);

        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOrderByIdAsync",
            Guid.NewGuid(),
            orderRepositoryMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task DeleteOrderAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteOrderAsync",
            Guid.NewGuid(),
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task DeleteOrderAsync_WhenOrderBelongsToDifferentUser_ShouldReturnForbid()
    {
        ApplicationUser user = new() { Id = Guid.NewGuid().ToString(), UserName = "user1" };
        MealOrder order = MealOrder.Create(Guid.NewGuid().ToString(), Guid.NewGuid(), DateTime.Today.AddDays(1), 10m);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteOrderAsync",
            order.Id,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "ForbidHttpResult");
    }

    [Fact]
    public async Task GetUserOrdersAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserOrdersAsync",
            null,
            null,
            null,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetUserOrderItemsAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserOrderItemsAsync",
            null,
            null,
            null,
            false,
            null,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetUserPaymentsAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserPaymentsAsync",
            null,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetUserPaymentSummaryAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserPaymentSummaryAsync",
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task MarkOrderAsPaidAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "MarkOrderAsPaidAsync",
            Guid.NewGuid(),
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetUserOrdersByMenuAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("user1");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserOrdersByMenuAsync",
            Guid.NewGuid(),
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        return new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object?[] parameters)
    {
        MethodInfo? methodInfo = typeof(OrdersEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(OrdersEndpoints).Assembly.GetType("Program")
            ?? throw new InvalidOperationException("Program type not found.");

        ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
        Type loggerType = typeof(Logger<>).MakeGenericType(programType);

        return Activator.CreateInstance(loggerType, loggerFactory)
            ?? throw new InvalidOperationException("Unable to create program logger.");
    }

    private static DefaultHttpContext CreateHttpContext(string userName)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userName)
            ],
            "TestAuth"));
        return httpContext;
    }

    private static void AssertHttpResultType(IResult result, string expectedTypeName)
    {
        Assert.Contains(expectedTypeName, result.GetType().Name, StringComparison.Ordinal);
    }
}
