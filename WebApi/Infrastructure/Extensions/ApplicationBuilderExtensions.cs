using WebApi.Routes.Emails;
using WebApi.Routes.Employees;
using WebApi.Routes.Menus;
using WebApi.Routes.Orders;
using WebApi.Routes.Reports;
using WebApi.Routes.Settings;
using WebApi.Routes.Suppliers;

namespace WebApi.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
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
        app.EmailEndpoints();
        app.MapSettingsEndpoints();
        app.MapAdminMenuEndpoints();
        app.MapReportEndpoints();
        return app;
    }

    public static WebApplication UseAppFallback(this WebApplication app)
    {
        app.UseAntiforgery();
        app.MapFallbackToFile("index.html");
        return app;
    }
}