using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Routes.Robots;

namespace ChemEats.Tests.WebApi.Routes.Robots;

public class RobotsEndpointsTests
{
    [Fact]
    public async Task GetRobotsTxt_WhenRequested_ShouldDisallowAllCrawlers()
    {
        RouteEndpoint endpoint = GetRobotsEndpoint();

        ServiceCollection services = new();
        services.AddLogging();
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = serviceProvider;
        using MemoryStream body = new();
        httpContext.Response.Body = body;

        await endpoint.RequestDelegate!(httpContext);

        string content = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.StartsWith("text/plain", httpContext.Response.ContentType);
        Assert.Contains("User-agent: *", content);
        Assert.Contains("Disallow: /", content);
    }

    [Fact]
    public void MapRobotsEndpoints_WhenMapped_ShouldAllowAnonymousGetOnly()
    {
        RouteEndpoint endpoint = GetRobotsEndpoint();

        HttpMethodMetadata? methodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        Assert.Equal(["GET"], methodMetadata.HttpMethods);
        Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
    }

    private static RouteEndpoint GetRobotsEndpoint()
    {
        WebApplication app = BuildAppWithRobotsEndpoints();

        IEndpointRouteBuilder endpointRouteBuilder = app;

        return endpointRouteBuilder.DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(routeEndpoint => string.Equals(routeEndpoint.RoutePattern.RawText, "robots.txt", StringComparison.OrdinalIgnoreCase));
    }

    private static WebApplication BuildAppWithRobotsEndpoints()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();

        WebApplication app = builder.Build();
        app.MapRobotsEndpoints();

        return app;
    }
}
