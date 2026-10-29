namespace WebApi.Infrastructure.Middleware;

public class NoIndexHeaderMiddleware
{
    public const string HeaderName = "X-Robots-Tag";
    public const string HeaderValue = "noindex, nofollow";

    private readonly RequestDelegate _next;

    public NoIndexHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers[HeaderName] = HeaderValue;
        await _next(context);
    }
}
