using Domain.Infrastructure.Identity;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Settings;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Orders;
using System.Reflection;
using WebApi.Routes.Orders;

namespace ChemEats.Tests.WebApi.Routes.Orders;

public class AdminOrdersEndpointsTests
{
    [Fact]
    public async Task OrderPayAsync_WhenOrderIdsAreEmpty_ShouldReturnBadRequest()
    {
        OrderPayRequestDto request = new("user-1", []);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<ISettingsRepository> settingsRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin");

        IResult result = await InvokePrivateResultMethodAsync(
            "OrderPayAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task OrderPayAsync_WhenAdminIsUnauthorized_ShouldReturnUnauthorized()
    {
        OrderPayRequestDto request = new("user-1", [Guid.NewGuid()]);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<ISettingsRepository> settingsRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin");

        IResult result = await InvokePrivateResultMethodAsync(
            "OrderPayAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task OrderPayAsync_WhenRequestIsValid_ShouldReturnOk()
    {
        Guid orderId = Guid.NewGuid();
        OrderPayRequestDto request = new("user-1", [orderId]);

        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.MarkOrderAsPaidAsync(
                "user-1",
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<DateTime>(),
                3m,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 7m));

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin");

        IResult result = await InvokePrivateResultMethodAsync(
            "OrderPayAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
    }

    [Fact]
    public async Task GetUnpaidOrdersAsync_ShouldReturnOk()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetUnpaidOrdersAsync("user-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUnpaidOrdersAsync",
            "user-1",
            null,
            orderRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
    }

    [Fact]
    public async Task GetOrdersForPeriodAsync_ShouldReturnOk()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetAllOrdersForPeriodAsync(
                "user-1",
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOrdersForPeriodAsync",
            "user-1",
            null,
            null,
            null,
            orderRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
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
        MethodInfo? methodInfo = typeof(AdminOrdersEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(AdminOrdersEndpoints).Assembly.GetType("Program")
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
