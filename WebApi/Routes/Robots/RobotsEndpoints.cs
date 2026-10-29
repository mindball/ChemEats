using Shared;

namespace WebApi.Routes.Robots;

public static class RobotsEndpoints
{
    public const string RobotsContent = "User-agent: *\nDisallow: /\n";

    public static void MapRobotsEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous by design: crawlers never authenticate, and without this route the SPA fallback
        // would answer /robots.txt with index.html. The response is static and exposes no data.
        app.MapGet(ApiRoutes.Robots.Base, GetRobotsTxt)
            .WithTags("Robots")
            .ExcludeFromDescription()
            .AllowAnonymous();
    }

    private static IResult GetRobotsTxt()
    {
        return Results.Text(RobotsContent, "text/plain");
    }
}
