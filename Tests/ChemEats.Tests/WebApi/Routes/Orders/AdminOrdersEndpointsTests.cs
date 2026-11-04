using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Obligations;
using Domain.Repositories.Settings;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
                admin.Id,
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

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenAdminIsUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<ISettingsRepository> settingsRepositoryMock = new();
        Mock<IMapper> mapperMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            null,
            null,
            orderRepositoryMock.Object,
            CreateEmptyObligationRepositoryMock().Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        orderRepositoryMock.Verify(repository => repository.GetOutstandingSummariesAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenEndDateIsBeforeStartDate_ShouldReturnBadRequest()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<ISettingsRepository> settingsRepositoryMock = new();
        Mock<IMapper> mapperMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateAdminUserManagerMock();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            new DateTime(2026, 10, 31),
            new DateTime(2026, 10, 1),
            null,
            null,
            orderRepositoryMock.Object,
            CreateEmptyObligationRepositoryMock().Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        orderRepositoryMock.Verify(repository => repository.GetOutstandingSummariesAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenDatesAreMissing_ShouldReturnBadRequest()
    {
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<ISettingsRepository> settingsRepositoryMock = new();
        Mock<IMapper> mapperMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateAdminUserManagerMock();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            null,
            new DateTime(2026, 10, 31),
            null,
            null,
            orderRepositoryMock.Object,
            CreateEmptyObligationRepositoryMock().Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenRangeIsValid_ShouldReturnOkWithMappedSummaries()
    {
        DateTime startDate = new(2026, 10, 1);
        DateTime endDate = new(2026, 10, 31);
        EmployeeOutstandingSummary summary = new("user-1", "User One", 2, 20m, 3m, 17m, 1, 5m);
        EmployeeOutstandingSummaryDto summaryDto = new("user-1", "User One", 2, 20m, 3m, 17m, 1, 5m);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetOutstandingSummariesAsync(
                startDate,
                endDate,
                3m,
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([summary]);

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<EmployeeOutstandingSummaryDto>(summary)).Returns(summaryDto);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateAdminUserManagerMock();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            startDate,
            endDate,
            "user-1",
            null,
            orderRepositoryMock.Object,
            CreateEmptyObligationRepositoryMock().Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<EmployeeOutstandingSummaryDto>> okResult = Assert.IsType<Ok<List<EmployeeOutstandingSummaryDto>>>(result);
        EmployeeOutstandingSummaryDto returned = Assert.Single(okResult.Value!);
        Assert.Equal(summaryDto, returned);
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenRequestIsValid_ShouldPayUpToDateAsAdminAndReturnOk()
    {
        DateTime upToDate = DateTime.Today.AddDays(-1);
        PayAllUnpaidRequestDto request = new("user-1", upToDate);
        ApplicationUser admin = new() { Id = "admin-id", UserName = "admin" };

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.MarkAllUnpaidAsPaidAsync(
                "user-1",
                upToDate,
                It.IsAny<DateTime>(),
                3m,
                "admin-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((4, 31.5m));

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<OrderPayResponseDto> okResult = Assert.IsType<Ok<OrderPayResponseDto>>(result);
        Assert.Equal(new OrderPayResponseDto(4, 31.5m), okResult.Value);
        orderRepositoryMock.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PayAllUnpaidAsync_WhenUserIdIsMissing_ShouldReturnBadRequest(string userId)
    {
        PayAllUnpaidRequestDto request = new(userId, DateTime.Today);
        Mock<IMealOrderRepository> orderRepositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidAsync",
            request,
            orderRepositoryMock.Object,
            new Mock<ISettingsRepository>().Object,
            CreateAdminUserManagerMock().Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        orderRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenDateIsInTheFuture_ShouldPayUpToThatDateAndReturnOk()
    {
        DateTime upToDate = DateTime.Today.AddDays(2);
        PayAllUnpaidRequestDto request = new("user-1", upToDate);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.MarkAllUnpaidAsPaidAsync(
                "user-1",
                upToDate,
                It.IsAny<DateTime>(),
                3m,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, 14m));

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            CreateAdminUserManagerMock().Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<OrderPayResponseDto> okResult = Assert.IsType<Ok<OrderPayResponseDto>>(result);
        Assert.Equal(new OrderPayResponseDto(2, 14m), okResult.Value);
        orderRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenAdminIsUnauthorized_ShouldReturnUnauthorized()
    {
        PayAllUnpaidRequestDto request = new("user-1", DateTime.Today);
        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidAsync",
            request,
            orderRepositoryMock.Object,
            new Mock<ISettingsRepository>().Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        orderRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenAllDatesIsSet_ShouldIgnoreDatesAndQueryWithoutBounds()
    {
        EmployeeOutstandingSummary summary = new("user-1", "User One", 5, 50m, 6m, 44m, 0, 0m);
        EmployeeOutstandingSummaryDto summaryDto = new("user-1", "User One", 5, 50m, 6m, 44m);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetOutstandingSummariesAsync(
                null,
                null,
                3m,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([summary]);

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<EmployeeOutstandingSummaryDto>(summary)).Returns(summaryDto);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            null,
            null,
            null,
            true,
            orderRepositoryMock.Object,
            CreateEmptyObligationRepositoryMock().Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            CreateAdminUserManagerMock().Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<EmployeeOutstandingSummaryDto>> okResult = Assert.IsType<Ok<List<EmployeeOutstandingSummaryDto>>>(result);
        Assert.Equal(summaryDto, Assert.Single(okResult.Value!));
        orderRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task PayAllUnpaidAsync_WhenDateIsNull_ShouldPayAllUnpaidRegardlessOfDate()
    {
        PayAllUnpaidRequestDto request = new("user-1", null);
        ApplicationUser admin = new() { Id = "admin-id", UserName = "admin" };

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.MarkAllUnpaidAsPaidAsync(
                "user-1",
                null,
                It.IsAny<DateTime>(),
                0m,
                "admin-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((7, 49m));

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidAsync",
            request,
            orderRepositoryMock.Object,
            settingsRepositoryMock.Object,
            userManagerMock.Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<OrderPayResponseDto> okResult = Assert.IsType<Ok<OrderPayResponseDto>>(result);
        Assert.Equal(new OrderPayResponseDto(7, 49m), okResult.Value);
        orderRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetOutstandingSummaryAsync_WhenEmployeesHaveOtherObligations_ShouldMergeThemAndAddObligationOnlyEmployees()
    {
        DateTime startDate = new(2026, 10, 1);
        DateTime endDate = new(2026, 10, 31);
        EmployeeOutstandingSummary mealSummary = new("user-2", "Beta User", 2, 20m, 3m, 17m, 0, 0m);
        EmployeeOutstandingSummaryDto mealSummaryDto = new("user-2", "Beta User", 2, 20m, 3m, 17m);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        orderRepositoryMock.Setup(repository => repository.GetOutstandingSummariesAsync(
                startDate,
                endDate,
                3m,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([mealSummary]);

        Mock<IOtherObligationRepository> obligationRepositoryMock = new();
        obligationRepositoryMock.Setup(repository => repository.GetOutstandingSummariesAsync(
                endDate,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EmployeeObligationSummary("user-1", "Alpha User", 1, 9m),
                new EmployeeObligationSummary("user-2", "Beta User", 2, 15.5m)
            ]);

        Mock<ISettingsRepository> settingsRepositoryMock = new();
        settingsRepositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3m);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<EmployeeOutstandingSummaryDto>(mealSummary)).Returns(mealSummaryDto);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetOutstandingSummaryAsync",
            startDate,
            endDate,
            null,
            null,
            orderRepositoryMock.Object,
            obligationRepositoryMock.Object,
            settingsRepositoryMock.Object,
            mapperMock.Object,
            CreateAdminUserManagerMock().Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<EmployeeOutstandingSummaryDto>> okResult = Assert.IsType<Ok<List<EmployeeOutstandingSummaryDto>>>(result);
        Assert.Equal(
        [
            new EmployeeOutstandingSummaryDto("user-1", "Alpha User", 0, 0m, 0m, 0m, 0, 0m, 1, 9m),
            new EmployeeOutstandingSummaryDto("user-2", "Beta User", 2, 20m, 3m, 17m, 0, 0m, 2, 15.5m)
        ], okResult.Value);
        obligationRepositoryMock.VerifyAll();
    }

    private static Mock<IOtherObligationRepository> CreateEmptyObligationRepositoryMock()
    {
        Mock<IOtherObligationRepository> obligationRepositoryMock = new();
        obligationRepositoryMock.Setup(repository => repository.GetOutstandingSummariesAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return obligationRepositoryMock;
    }

    private static Mock<UserManager<ApplicationUser>> CreateAdminUserManagerMock()
    {
        ApplicationUser admin = new() { Id = Guid.NewGuid().ToString(), UserName = "admin" };

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(admin);

        return userManagerMock;
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
