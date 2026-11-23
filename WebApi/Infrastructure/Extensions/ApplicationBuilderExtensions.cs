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
        app.UseSwagger();

        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChemEats API v1");
                options.RoutePrefix = "swagger";
            });
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseSwaggerUI();
            // Protect Swagger in non-dev
            app.MapSwagger().RequireAuthorization();
        }

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
        app.UseCors("Open");
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapIdentityApi<Domain.Infrastructure.Identity.ApplicationUser>();
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