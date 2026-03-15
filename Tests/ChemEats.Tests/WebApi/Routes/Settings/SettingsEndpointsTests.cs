using System.Reflection;
using Domain.Repositories.Settings;
using Microsoft.AspNetCore.Http;
using Moq;
using WebApi.Routes.Settings;

namespace ChemEats.Tests.WebApi.Routes.Settings;

public class SettingsEndpointsTests
{
    [Fact]
    public async Task GetCompanyPortionAsync_ShouldReturnOkResult()
    {
        Mock<ISettingsRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetCompanyPortionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(4.5m);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("tester");

        IResult result = await InvokePrivateResultMethodAsync(
            "GetCompanyPortionAsync",
            repositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
    }

    [Fact]
    public async Task UpdateCompanyPortionAsync_WhenAmountIsNegative_ShouldReturnBadRequest()
    {
        Mock<ISettingsRepository> repositoryMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("tester");

        PortionRequest request = new(-1m);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateCompanyPortionAsync",
            repositoryMock.Object,
            request,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.Verify(repository => repository.SetCompanyPortionAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCompanyPortionAsync_WhenAmountIsValid_ShouldReturnNoContent()
    {
        Mock<ISettingsRepository> repositoryMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("tester");

        PortionRequest request = new(5m);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateCompanyPortionAsync",
            repositoryMock.Object,
            request,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "NoContent");
        repositoryMock.Verify(repository => repository.SetCompanyPortionAsync(5m, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(SettingsEndpoints).Assembly.GetType("Program")
            ?? throw new InvalidOperationException("Program type not found.");

        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
        Type loggerType = typeof(Microsoft.Extensions.Logging.Logger<>).MakeGenericType(programType);

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

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object[] parameters)
    {
        MethodInfo? methodInfo = typeof(SettingsEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static void AssertHttpResultType(IResult result, string expectedTypeName)
    {
        Assert.Contains(expectedTypeName, result.GetType().Name, StringComparison.Ordinal);
    }
}
