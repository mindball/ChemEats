using System.Reflection;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Mappings;
using Shared.DTOs.Reports;
using WebApi.Routes.Reports;

namespace ChemEats.Tests.WebApi.Routes.Reports;

public class ReportEndpointsTests
{
    [Fact]
    public async Task GetMenuReportDataAsync_WhenUserUnauthorized_ShouldReturnUnauthorized()
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
            "GetMenuReportDataAsync",
            menuId,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            CreateMapper(),
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        orderRepositoryMock.Verify(
            repository => repository.GetAllOrdersByMenuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMenuReportDataAsync_WhenMenuMissing_ShouldReturnNotFound()
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
            "GetMenuReportDataAsync",
            menuId,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            CreateMapper(),
            httpContext,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task GetMenuReportDataAsync_WhenMenuExists_ShouldReturnReportWithOrders()
    {
        Guid supplierId = Guid.NewGuid();
        Meal soup = Meal.Create(Guid.NewGuid(), "Супа топчета", new Price(4.50m));
        Meal salad = Meal.Create(Guid.NewGuid(), "Shopska salad", new Price(6m));
        Menu menu = Menu.Create(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [soup, salad]);

        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        List<UserOrderItem> orders = [
            new UserOrderItem(
                Guid.NewGuid(), "user1", "Иван Петров", soup.Id, soup.Name,
                supplierId, "Test Supplier", DateTime.UtcNow, menu.Date,
                soup.Price.Amount, "Pending", false, false, 0, soup.Price.Amount, false, null),
            new UserOrderItem(
                Guid.NewGuid(), "user2", "John Doe", salad.Id, salad.Name,
                supplierId, "Test Supplier", DateTime.UtcNow, menu.Date,
                salad.Price.Amount, "Completed", false, false, 0, salad.Price.Amount, false, null)
        ];
        orderRepositoryMock.Setup(repository => repository.GetAllOrdersByMenuAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        DefaultHttpContext httpContext = CreateHttpContext("admin");
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMenuReportDataAsync",
            menu.Id,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            CreateMapper(),
            httpContext,
            logger,
            CancellationToken.None);

        Ok<MenuReportDto> okResult = Assert.IsType<Ok<MenuReportDto>>(result);
        MenuReportDto report = Assert.IsType<MenuReportDto>(okResult.Value);
        Assert.Equal(menu.Id, report.MenuId);
        Assert.Equal(menu.Date, report.MenuDate);
        Assert.Equal(menu.ActiveUntil, report.ActiveUntil);
        Assert.Equal(2, report.Orders.Count);
        Assert.Equal(new MenuReportOrderDto("Иван Петров", "Супа топчета", 4.50m), report.Orders[0]);
        Assert.Equal(new MenuReportOrderDto("John Doe", "Shopska salad", 6m), report.Orders[1]);
    }

    [Fact]
    public async Task GetMenuReportDataAsync_WhenMenuHasNoOrders_ShouldReturnReportWithEmptyOrders()
    {
        Meal meal = Meal.Create(Guid.NewGuid(), "Soup", new Price(10m));
        Menu menu = Menu.Create(
            Guid.NewGuid(),
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [meal]);

        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetAllOrdersByMenuAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMenuReportDataAsync",
            menu.Id,
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<MenuReportDto> okResult = Assert.IsType<Ok<MenuReportDto>>(result);
        Assert.Empty(okResult.Value!.Orders);
    }

    private static IMapper CreateMapper()
    {
        TypeAdapterConfig config = new();
        new MenuReportMappings().Register(config);
        return new Mapper(config);
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
