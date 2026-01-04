using Domain.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Shared.DTOs.Errors;
using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
    .AddAppIdentity()
    .AddAppJwt(builder.Configuration)
    .AddAppRazorComponents()
    .AddAppRepositories()
    .AddAppMapster()
    .AddAppEmployees(builder.Configuration);

WebApplication app = builder.Build();

// Middleware
app.UseAppSwagger(app.Environment)
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

// Startup tasks (cache + sync)
using (IServiceScope scope = app.Services.CreateScope())
{
    IEmployeeCacheService employeeCache = scope.ServiceProvider.GetRequiredService<IEmployeeCacheService>();
    IEmployeeSyncService syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    try
    {
        await syncService.SyncEmployeesAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Employee sync failed on startup");
    }
    finally
    {
        await employeeCache.InitializeAsync();

        app.UseAppFallback();
    }
}

await app.RunAsync();
