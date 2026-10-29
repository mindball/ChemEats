using System.Reflection;
using System.Security.Claims;
using ChemEats.Tests.TestInfrastructure;
using Domain.Infrastructure.Identity;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Mappings;
using Shared.DTOs.Notifications;
using WebApi.Routes.Notifications;

namespace ChemEats.Tests.WebApi.Routes.Notifications;

public class NotificationEndpointsTests
{
    [Fact]
    public async Task GetMyNotificationSettingsAsync_WhenUserIsUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMyNotificationSettingsAsync",
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("user1"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task GetMyNotificationSettingsAsync_WhenUserExists_ShouldReturnOwnSettings()
    {
        ApplicationUser user = TestDataFactory.CreateUserWithReminders("u1", "MM", "Mincho", "personal@example.com");
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMyNotificationSettingsAsync",
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<NotificationSettingsDto> ok = Assert.IsType<Ok<NotificationSettingsDto>>(result);
        Assert.Equal(new NotificationSettingsDto("personal@example.com", true), ok.Value);
    }

    [Fact]
    public async Task UpdateMyNotificationSettingsAsync_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            null,
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        userManagerMock.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyNotificationSettingsAsync_WhenUserIsUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            new NotificationSettingsDto("personal@example.com", true),
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Name <name@example.com>")]
    public async Task UpdateMyNotificationSettingsAsync_WhenEmailIsInvalid_ShouldReturnBadRequest(string email)
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            new NotificationSettingsDto(email, true),
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        Assert.Null(user.NotificationEmail);
        userManagerMock.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyNotificationSettingsAsync_WhenRemindersEnabledWithoutEmail_ShouldReturnBadRequest()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            new NotificationSettingsDto("  ", true),
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        userManagerMock.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyNotificationSettingsAsync_WhenValid_ShouldUpdateCurrentUserAndReturnOk()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(manager => manager.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            new NotificationSettingsDto(" personal@example.com ", true),
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<NotificationSettingsDto> ok = Assert.IsType<Ok<NotificationSettingsDto>>(result);
        Assert.Equal(new NotificationSettingsDto("personal@example.com", true), ok.Value);
        Assert.Equal("personal@example.com", user.NotificationEmail);
        Assert.True(user.OrderRemindersEnabled);
        Assert.Equal("mm@cpachem.com", user.Email);
        userManagerMock.Verify(manager => manager.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateMyNotificationSettingsAsync_WhenUpdateFails_ShouldReturnBadRequest()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(manager => manager.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency failure" }));

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMyNotificationSettingsAsync",
            new NotificationSettingsDto("personal@example.com", false),
            userManagerMock.Object,
            CreateMapper(),
            CreateHttpContext("MM"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    private static IMapper CreateMapper()
    {
        TypeAdapterConfig config = new();
        new NotificationMappings().Register(config);
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
        MethodInfo? methodInfo = typeof(NotificationEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(NotificationEndpoints).Assembly.GetType("Program")
            ?? throw new InvalidOperationException("Program type not found.");

        ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
        Type loggerType = typeof(Logger<>).MakeGenericType(programType);

        return Activator.CreateInstance(loggerType, loggerFactory)
            ?? throw new InvalidOperationException("Unable to create program logger.");
    }

    private static DefaultHttpContext CreateHttpContext(string userName)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, userName)
            ],
            "TestAuth"));
        return httpContext;
    }

    private static void AssertHttpResultType(IResult result, string expectedTypeName)
    {
        Assert.Contains(expectedTypeName, result.GetType().Name, StringComparison.Ordinal);
    }
}
