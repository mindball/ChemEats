using Microsoft.AspNetCore.Http;
using WebApi.Infrastructure.Middleware;

namespace ChemEats.Tests.WebApi.Infrastructure.Middleware;

public class NoIndexHeaderMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestSucceeds_ShouldAddNoIndexHeaderAndCallNext()
    {
        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        NoIndexHeaderMiddleware middleware = new(next);
        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/index.html";

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        Assert.Equal("noindex, nofollow", httpContext.Response.Headers["X-Robots-Tag"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextReturnsNotFound_ShouldStillAddNoIndexHeader()
    {
        RequestDelegate next = context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        };

        NoIndexHeaderMiddleware middleware = new(next);
        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/api/unknown";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("noindex, nofollow", httpContext.Response.Headers["X-Robots-Tag"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldPropagateException()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Pipeline failed");

        NoIndexHeaderMiddleware middleware = new(next);
        DefaultHttpContext httpContext = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(httpContext));
        Assert.Equal("noindex, nofollow", httpContext.Response.Headers["X-Robots-Tag"].ToString());
    }
}
