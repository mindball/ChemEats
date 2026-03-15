using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebApi.Routes.Emails;

namespace ChemEats.Tests.WebApi.Routes.Emails;

public class TestEmailEndpointsTests
{
    [Fact]
    public async Task TestEmailEndpoint_WhenSenderSucceeds_ShouldReturnOk()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();

        Mock<IEmailSender<ApplicationUser>> emailSenderMock = new();
        emailSenderMock.Setup(sender => sender.SendConfirmationLinkAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        WebApplication app = builder.Build();

        app.EmailEndpoints();

        IEndpointRouteBuilder endpointRouteBuilder = app;
        IEnumerable<EndpointDataSource> endpointDataSources = endpointRouteBuilder.DataSources;

        RouteEndpoint endpoint = endpointDataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(routeEndpoint => string.Equals(routeEndpoint.RoutePattern.RawText, "test-email", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(routeEndpoint.RoutePattern.RawText, "/test-email", StringComparison.OrdinalIgnoreCase));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(emailSenderMock.Object);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = serviceProvider;

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TestEmailEndpoint_WhenSenderThrows_ShouldReturnBadRequest()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();

        Mock<IEmailSender<ApplicationUser>> emailSenderMock = new();
        emailSenderMock.Setup(sender => sender.SendConfirmationLinkAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP failed"));

        WebApplication app = builder.Build();
        app.EmailEndpoints();

        IEndpointRouteBuilder endpointRouteBuilder = app;
        IEnumerable<EndpointDataSource> endpointDataSources = endpointRouteBuilder.DataSources;

        RouteEndpoint endpoint = endpointDataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(routeEndpoint => string.Equals(routeEndpoint.RoutePattern.RawText, "test-email", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(routeEndpoint.RoutePattern.RawText, "/test-email", StringComparison.OrdinalIgnoreCase));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(emailSenderMock.Object);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = serviceProvider;

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }
}
