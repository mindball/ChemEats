using Domain.Repositories.Settings;

namespace WebApi.Routes.Settings;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").RequireAuthorization();

        group.MapGet("/portion", async (ISettingsRepository repo, CancellationToken ct) =>
        {
            var portion = await repo.GetCompanyPortionAsync(ct);
            return Results.Ok(new { PortionAmount = portion });
        });

        group.MapPut("/portion", async (ISettingsRepository repo, PortionRequest req, CancellationToken ct) =>
        {
            if (req.PortionAmount < 0)
                return Results.BadRequest("Portion amount must be >= 0.");

            await repo.SetCompanyPortionAsync(req.PortionAmount, ct);
            return Results.NoContent();
        });
    }

    public sealed record PortionRequest(decimal PortionAmount);
}