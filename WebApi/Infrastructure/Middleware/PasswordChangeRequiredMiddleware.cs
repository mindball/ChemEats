using Shared;
using Shared.DTOs.Errors;
using WebApi.Infrastructure.Identity;

namespace WebApi.Infrastructure.Middleware;

// Blocks every API call except "change my password" while the token carries the must-change-password claim,
// so a temporary password cannot be used to work with the application.
public class PasswordChangeRequiredMiddleware
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString ChangePasswordPath = new("/" + ApiRoutes.Employees.ChangeMyPassword());

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool mustChangePassword = context.User.HasClaim(
            PasswordChangeRequirement.ClaimType,
            PasswordChangeRequirement.ClaimValue);

        if (mustChangePassword
            && context.Request.Path.StartsWithSegments(ApiPrefix)
            && !context.Request.Path.Equals(ChangePasswordPath, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetailsDto(
                Title: "Password change required",
                Detail: "You must change your temporary password before using the application.",
                Status: StatusCodes.Status403Forbidden,
                Type: "https://httpstatuses.com/403"));
            return;
        }

        await _next(context);
    }
}
