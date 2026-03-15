using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApi.Infrastructure.Filters;

namespace ChemEats.Tests.WebApi.Infrastructure.Filters;

public class AuthorizedRequestLoggingFilterTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestIsUnauthenticated_ShouldCallNext()
    {
        AuthorizedRequestLoggingFilter filter = new();
        DefaultHttpContext httpContext = new();
        TestEndpointFilterInvocationContext context = new(httpContext);

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        object? result = await filter.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsAuthenticated_ShouldCallNext()
    {
        AuthorizedRequestLoggingFilter filter = new();
        DefaultHttpContext httpContext = new();
        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "test-user")], "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/test";

        TestEndpointFilterInvocationContext context = new(httpContext);

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        object? result = await filter.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotNull(result);
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        private readonly object?[] _arguments;

        public TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] arguments)
        {
            HttpContext = httpContext;
            _arguments = arguments;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments => _arguments;

        public override T GetArgument<T>(int index)
        {
            return (T)_arguments[index]!;
        }
    }
}
