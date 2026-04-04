using System.Reflection;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using QuestPDF.Infrastructure;
using WebApi.Routes.Reports;

namespace ChemEats.Tests.WebApi.Routes.Reports;

public class ReportEndpointsTests
{
    [Fact]
    public async Task GenerateMenuReportAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
    {
        Guid menuId = Guid.NewGuid();

        Mock<IMenuRepository> menuRepositoryMock = new();
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        DefaultHttpContext httpContext = CreateHttpContext("admin");
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GenerateMenuReportAsync",
            menuId,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GenerateMenuReportAsync_WhenMenuMissing_ShouldReturnNotFound()
    {
        Guid menuId = Guid.NewGuid();

        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Menu?)null);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        DefaultHttpContext httpContext = CreateHttpContext("admin");
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GenerateMenuReportAsync",
            menuId,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task GenerateMenuReportAsync_WhenMenuExists_ShouldReturnFileResult()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Guid supplierId = Guid.NewGuid();
        Meal meal = Meal.Create(Guid.NewGuid(), "Soup", new Price(10m));
        Menu menu = Menu.Create(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [meal]);

        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        List<UserOrderItem> orders = [
            new UserOrderItem(
                Guid.NewGuid(), "user1", "John Doe", meal.Id, meal.Name,
                supplierId, "Test Supplier", DateTime.UtcNow, menu.Date,
                meal.Price.Amount, "Pending", false, false, 0, meal.Price.Amount)
        ];
        orderRepositoryMock.Setup(repository => repository.GetAllOrdersByMenuAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        DefaultHttpContext httpContext = CreateHttpContext("admin");
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GenerateMenuReportAsync",
            menu.Id,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "File");
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
        MethodInfo? methodInfo = typeof(ReportEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(ReportEndpoints).Assembly.GetType("Program")
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
