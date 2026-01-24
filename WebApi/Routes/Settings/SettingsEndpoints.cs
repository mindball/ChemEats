using Domain.Repositories.Settings;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Settings;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/settings").RequireAuthorization();

        group.MapGet("/portion", async (
            ISettingsRepository repo,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                logger.LogInformation(
                    "User {User} retrieving company portion setting",
                    context.User.Identity?.Name);

                decimal portion = await repo.GetCompanyPortionAsync(ct);

                logger.LogInformation(
                    "Company portion retrieved: {PortionAmount} by {User}",
                    portion,
                    context.User.Identity?.Name);

                return Results.Ok(new { PortionAmount = portion });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error retrieving company portion: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPut("/portion", async (
            ISettingsRepository repo,
            PortionRequest req,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                if (req.PortionAmount < 0)
                {
                    logger.LogWarning(
                        "Invalid portion amount {PortionAmount} rejected by {User}",
                        req.PortionAmount,
                        context.User.Identity?.Name);
                    return Results.BadRequest("Portion amount must be >= 0.");
                }

                logger.LogInformation(
                    "User {User} updating company portion from current value to {NewPortionAmount}",
                    context.User.Identity?.Name,
                    req.PortionAmount);

                await repo.SetCompanyPortionAsync(req.PortionAmount, ct);

                logger.LogInformation(
                    "Company portion successfully updated to {PortionAmount} by {User}",
                    req.PortionAmount,
                    context.User.Identity?.Name);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error updating company portion to {PortionAmount}: {ErrorMessage}",
                    req.PortionAmount,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }
}

public sealed record PortionRequest(decimal PortionAmount);