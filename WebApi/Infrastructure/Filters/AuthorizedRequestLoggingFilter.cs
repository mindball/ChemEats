namespace WebApi.Infrastructure.Filters;

using Serilog;

public class AuthorizedRequestLoggingFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var user = httpContext.User.Identity?.Name ?? "UnknownUser";
            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method;

            Log.Information("Entering {Method} {Path} by {User}", method, path, user);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await next(context);
            sw.Stop();

            Log.Information("Exiting {Method} {Path} by {User} with status {StatusCode} in {ElapsedMs} ms",
                method, path, user, httpContext.Response.StatusCode, sw.ElapsedMilliseconds);

            return result;
        }

        return await next(context);
    }
}
