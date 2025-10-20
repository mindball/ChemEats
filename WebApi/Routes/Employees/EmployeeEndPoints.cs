using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Employees;

public static class EmployeeEndPoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        app.MapPost("/api/sync-employees", async (IEmployeeSyncService syncService) =>
            {
                await syncService.SyncEmployeesAsync();
                return Results.Ok("Employees synchronized successfully.");
            })
            .RequireAuthorization("AdminPolicy") // или просто .RequireAuthorization()
            .WithTags("System Maintenance")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }
}