using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebApi.Infrastructure.Middleware;

namespace ChemEats.Tests.WebApi.Infrastructure.Middleware;

public class RequireIdentityAuthorizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenPublicRoute_ShouldCallNext()
    {
        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        Mock<ILogger<RequireIdentityAuthorizationMiddleware>> loggerMock = new();
        RequireIdentityAuthorizationMiddleware middleware = new(next, loggerMock.Object);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/swagger/index.html";

        Mock<IAuthorizationService> authorizationServiceMock = new();

        await middleware.InvokeAsync(httpContext, authorizationServiceMock.Object);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenProtectedAccountRouteAndUserUnauthenticated_ShouldReturnUnauthorized()
    {
        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        Mock<ILogger<RequireIdentityAuthorizationMiddleware>> loggerMock = new();
        RequireIdentityAuthorizationMiddleware middleware = new(next, loggerMock.Object);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/api/account/manage";

        Mock<IAuthorizationService> authorizationServiceMock = new();

        await middleware.InvokeAsync(httpContext, authorizationServiceMock.Object);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenProtectedAccountRouteAndUserAuthenticated_ShouldCallNext()
    {
        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        Mock<ILogger<RequireIdentityAuthorizationMiddleware>> loggerMock = new();
        RequireIdentityAuthorizationMiddleware middleware = new(next, loggerMock.Object);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/api/account/manage";
        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "user1")], "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        Mock<IAuthorizationService> authorizationServiceMock = new();

        await middleware.InvokeAsync(httpContext, authorizationServiceMock.Object);

        Assert.True(nextCalled);
    }
}
