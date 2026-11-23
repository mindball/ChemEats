using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Middleware;
using WebApi.Routes.Emails;
using WebApi.Routes.Employees;
using WebApi.Routes.Menus;
using WebApi.Routes.Notifications;
using WebApi.Routes.Obligations;
using WebApi.Routes.Orders;
using WebApi.Routes.Reports;
using WebApi.Routes.Robots;
using WebApi.Routes.Settings;
using WebApi.Routes.Suppliers;

namespace WebApi.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseAppNoIndex(this WebApplication app)
    {
        app.UseMiddleware<NoIndexHeaderMiddleware>();
        return app;
    }

    public static WebApplication UseAppSwagger(this WebApplication app, IHostEnvironment env)
    {
        // Swagger exposes the whole API surface, so it is only available in Development.
        if (!env.IsDevelopment())
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChemEats API v1");
            options.RoutePrefix = "swagger";
        });
        app.UseWebAssemblyDebugging();

        return app;
    }

    public static WebApplication UseAppWebAssets(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        return app;
    }

    public static WebApplication UseAppSecurity(this WebApplication app)
    {
        // CORS, Authentication, Authorization
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<PasswordChangeRequiredMiddleware>();
        return app;
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        // MapIdentityApi is intentionally not mapped: it exposes an anonymous /register endpoint.
        app.MapMenuEndpoints();
        app.MapSupplierEndpoints();
        app.MapEmployeeEndpoints();
        app.MapMealOrderEndpoints();
        app.MapAdminMealOrderEndpoints();
        app.MapObligationEndpoints();
        app.MapAdminObligationEndpoints();
        app.MapAdminObligationTypeEndpoints();
        app.EmailEndpoints();
        app.MapSettingsEndpoints();
        app.MapAdminMenuEndpoints();
        app.MapReportEndpoints();
        app.MapNotificationEndpoints();
        app.MapRobotsEndpoints();
        return app;
    }

    public static async Task<WebApplication> SeedAppIdentityAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IIdentitySeedService seedService = scope.ServiceProvider.GetRequiredService<IIdentitySeedService>();
        ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await seedService.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Identity seeding failed on startup");
        }

        return app;
    }

    public static WebApplication UseAppFallback(this WebApplication app)
    {
        app.UseAntiforgery();
        app.MapFallbackToFile("index.html");
        return app;
    }
}