using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Middleware;

namespace ChemEats.Tests.WebApi.Infrastructure.Middleware;

public class PasswordChangeRequiredMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenPasswordChangeRequiredAndApiCalled_ShouldReturnForbidden()
    {
        (PasswordChangeRequiredMiddleware middleware, Func<bool> nextCalled) = CreateMiddleware();
        DefaultHttpContext context = CreateContext("/api/orders", mustChangePassword: true);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled());
    }

    [Fact]
    public async Task InvokeAsync_WhenPasswordChangeRequiredAndChangingPassword_ShouldContinue()
    {
        (PasswordChangeRequiredMiddleware middleware, Func<bool> nextCalled) = CreateMiddleware();
        DefaultHttpContext context = CreateContext("/" + ApiRoutes.Employees.ChangeMyPassword(), mustChangePassword: true);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled());
    }

    [Fact]
    public async Task InvokeAsync_WhenPasswordChangeNotRequired_ShouldContinue()
    {
        (PasswordChangeRequiredMiddleware middleware, Func<bool> nextCalled) = CreateMiddleware();
        DefaultHttpContext context = CreateContext("/api/orders", mustChangePassword: false);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled());
    }

    private static (PasswordChangeRequiredMiddleware Middleware, Func<bool> NextCalled) CreateMiddleware()
    {
        bool called = false;
        PasswordChangeRequiredMiddleware middleware = new(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        return (middleware, () => called);
    }

    private static DefaultHttpContext CreateContext(string path, bool mustChangePassword)
    {
        List<Claim> claims = [new Claim(ClaimTypes.Name, "MM")];
        if (mustChangePassword)
            claims.Add(new Claim(PasswordChangeRequirement.ClaimType, PasswordChangeRequirement.ClaimValue));

        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"))
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
