    using Domain.Infrastructure.Exceptions;
using MenuParser.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Shared.DTOs.Errors;
using WebApi.Infrastructure.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ChemEats WebApi";
});

// Each is a no-op unless the process is started by that service manager.
builder.Host.UseSystemd();

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddAppOpenApi()
    .AddAppCors()
    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
    .AddAppDb(builder.Configuration)
    .AddAppEmail(builder.Configuration)
    .AddAppNotifications(builder.Configuration)
    .AddAppMenuAutoFinalize(builder.Configuration)
    .AddAppIdentity()
    .AddAppJwt(builder.Configuration)
    .AddAppRazorComponents()
    .AddAppRepositories()
    .AddAppMapster()
    .AddAppEmployees(builder.Configuration)
    .AddMenuParser(builder.Configuration);

WebApplication app = builder.Build();

// Middleware
app.UseAppNoIndex()
   .UseAppSwagger(app.Environment)
   .UseAppWebAssets()
   .UseAppSecurity()
   .MapAppEndpoints();

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is DomainException domainEx)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetailsDto(
                Title: "Business rule violation",
                Detail: domainEx.Message,
                Status: StatusCodes.Status409Conflict,
                Type: "https://httpstatuses.com/409"
            ));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});

// Startup tasks (roles + optional bootstrap admin)
await app.SeedAppIdentityAsync();
app.UseAppFallback();

await app.RunAsync();
