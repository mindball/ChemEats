using Serilog;
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

// Startup tasks (cache + sync)
using (IServiceScope scope = app.Services.CreateScope())
{
    IEmployeeCacheService employeeCache = scope.ServiceProvider.GetRequiredService<IEmployeeCacheService>();
    await employeeCache.InitializeAsync();

    IEmployeeSyncService syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();
    await syncService.SyncEmployeesAsync();
}

app.UseAppFallback();

await app.RunAsync();
