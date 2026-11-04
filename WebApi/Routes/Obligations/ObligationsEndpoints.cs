using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Obligations;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Obligations;

public static class ObligationsEndpoints
{
    public static void MapObligationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.Obligations.Base)
            .RequireAuthorization();

        group.MapGet(ApiRoutes.Obligations.Me, GetMyObligationsAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetMyObligationsAsync(
        DateTime? startDate,
        DateTime? endDate,
        IOtherObligationRepository obligationRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Get my obligations rejected - unauthorized user");
                return Results.Unauthorized();
            }

            if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
            {
                logger.LogWarning(
                    "Get my obligations rejected for user {UserId} - end date {EndDate} is before start date {StartDate}",
                    user.Id,
                    endDate.Value.ToString("yyyy-MM-dd"),
                    startDate.Value.ToString("yyyy-MM-dd"));
                return Results.BadRequest("End date cannot be before start date.");
            }

            IReadOnlyList<OtherObligationItem> items = await obligationRepository.GetForUserAsync(
                user.Id,
                startDate,
                endDate,
                cancellationToken: cancellationToken);

            List<OtherObligationDto> dtos = items.Select(mapper.Map<OtherObligationDto>).ToList();

            logger.LogInformation(
                "User {UserName} ({UserId}) retrieved {ObligationCount} other obligations - Period: {StartDate} to {EndDate}",
                user.UserName,
                user.Id,
                dtos.Count,
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None");

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving own other obligations: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
