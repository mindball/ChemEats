using Microsoft.AspNetCore.Authorization;

namespace WebApi.Infrastructure.Middleware;

public class RequireIdentityAuthorizationMiddleware
{
    private readonly ILogger<RequireIdentityAuthorizationMiddleware> _logger;
    private readonly RequestDelegate _next;

    // Списък с публични пътища – зададен директно в кода
    private static readonly string[] PublicPrefixes =
    {
        "/swagger",          // Swagger UI
        "/api/account/login",
        "/api/account/register",
        "/api/account/forgotpassword",
        "/api/account/resetpassword",
        "/api/info",          // примерен Info endpoint
        "/index.html",
        "/_framework",
        "/_content",
        "/favicon.ico"
    };

    public RequireIdentityAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<RequireIdentityAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        _logger.LogInformation("Initialized RequireIdentityAuthorizationMiddleware with {Count} public prefixes: {Prefixes}",
            PublicPrefixes.Length, string.Join(", ", PublicPrefixes));
    }

    public async Task InvokeAsync(HttpContext context, IAuthorizationService authorizationService)
    {
        string path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        string? user = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous";

        // Проверка дали текущият път съвпада с някой от публичните
        if (PublicPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Public route accessed: {Path} by {User}", path, user);
            await _next(context);
            return;
        }

        // Проверка за account/manage и т.н.
        if (path.StartsWith("/api/account") || path.StartsWith("/manage"))
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Unauthorized access attempt to {Path} from IP {IP}", path,
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            _logger.LogInformation("Authorized access to {Path} by {User}", path, user);
        }

        await _next(context);
    }
}

public static class RequireIdentityAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequireIdentityAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequireIdentityAuthorizationMiddleware>();
    }
}
