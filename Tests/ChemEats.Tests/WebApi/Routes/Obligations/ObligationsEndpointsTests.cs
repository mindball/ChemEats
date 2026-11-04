using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Obligations;
using System.Reflection;
using WebApi.Routes.Obligations;

namespace ChemEats.Tests.WebApi.Routes.Obligations;

public class ObligationsEndpointsTests
{
    [Fact]
    public async Task GetMyObligationsAsync_WhenUserIsAuthenticated_ShouldQueryOnlyCurrentUser()
    {
        ApplicationUser user = new() { Id = "user-1", UserName = "U1" };
        DateTime startDate = new(2026, 10, 1);
        DateTime endDate = new(2026, 10, 31);
        Guid obligationId = Guid.NewGuid();

        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetForUserAsync("user-1", startDate, endDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new OtherObligationItem(obligationId, "user-1", "User One", null, "Toner", 12.5m, startDate, DateTime.Now, "Admin", PaymentStatus.Unpaid, null, null)
            ]);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<OtherObligationDto>(It.IsAny<OtherObligationItem>()))
            .Returns(new OtherObligationDto(obligationId, "user-1", "User One", null, "Toner", 12.5m, startDate, DateTime.Now, "Admin", PaymentStatusDto.Unpaid, null, null));

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMyObligationsAsync",
            startDate,
            endDate,
            repositoryMock.Object,
            CreateUserManagerMock(user).Object,
            CreateHttpContext("U1"),
            mapperMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<OtherObligationDto>> okResult = Assert.IsType<Ok<List<OtherObligationDto>>>(result);
        Assert.Equal(obligationId, Assert.Single(okResult.Value!).Id);
        repositoryMock.Verify(
            repository => repository.GetForUserAsync(It.Is<string>(id => id != "user-1"), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<PaymentStatus?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMyObligationsAsync_WhenUserIsUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMyObligationsAsync",
            null,
            null,
            repositoryMock.Object,
            CreateUserManagerMock(null).Object,
            CreateHttpContext("U1"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyObligationsAsync_WhenEndDateIsBeforeStartDate_ShouldReturnBadRequest()
    {
        ApplicationUser user = new() { Id = "user-1", UserName = "U1" };
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMyObligationsAsync",
            new DateTime(2026, 10, 31),
            new DateTime(2026, 10, 1),
            repositoryMock.Object,
            CreateUserManagerMock(user).Object,
            CreateHttpContext("U1"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser? currentUser)
    {
        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = new(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(currentUser);

        return userManagerMock;
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object?[] parameters)
    {
        MethodInfo? methodInfo = typeof(ObligationsEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(ObligationsEndpoints).Assembly.GetType("Program")
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
