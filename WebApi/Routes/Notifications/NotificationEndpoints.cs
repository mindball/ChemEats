using Domain.Infrastructure.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Notifications;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.Notifications.Base)
            .RequireAuthorization()
            .WithTags("Notifications");

        group.MapGet(ApiRoutes.Notifications.Me, GetMyNotificationSettingsAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPut(ApiRoutes.Notifications.Me, UpdateMyNotificationSettingsAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetMyNotificationSettingsAsync(
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Get notification settings rejected - unauthorized user");
                return Results.Unauthorized();
            }

            return Results.Ok(mapper.Map<NotificationSettingsDto>(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notification settings for {User}", httpContext.User.Identity?.Name);
            return Results.Problem("An error occurred while retrieving notification settings.");
        }
    }

    private static async Task<IResult> UpdateMyNotificationSettingsAsync(
        NotificationSettingsDto? request,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
            {
                logger.LogWarning("Update notification settings rejected - empty request");
                return Results.BadRequest("Request body is required.");
            }

            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Update notification settings rejected - unauthorized user");
                return Results.Unauthorized();
            }

            string? email = string.IsNullOrWhiteSpace(request.NotificationEmail) ? null : request.NotificationEmail.Trim();

            if (email is not null && !ApplicationUser.IsValidEmail(email))
            {
                logger.LogWarning("Update notification settings rejected for user {UserId} - invalid email format", user.Id);
                return Results.BadRequest("Please enter a valid email address.");
            }

            if (request.OrderRemindersEnabled && email is null)
            {
                logger.LogWarning("Update notification settings rejected for user {UserId} - reminders enabled without email", user.Id);
                return Results.BadRequest("An email address is required to enable order reminders.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            user.UpdateNotificationSettings(email, request.OrderRemindersEnabled);

            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(error => error.Description));
                logger.LogWarning("Update notification settings failed for user {UserId}: {Errors}", user.Id, errors);
                return Results.BadRequest(errors);
            }

            logger.LogInformation(
                "User {UserName} ({UserId}) updated notification settings (reminders enabled: {RemindersEnabled})",
                user.UserName,
                user.Id,
                user.OrderRemindersEnabled);

            return Results.Ok(mapper.Map<NotificationSettingsDto>(user));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update notification settings for {User}", httpContext.User.Identity?.Name);
            return Results.Problem("An error occurred while updating notification settings.");
        }
    }
}
