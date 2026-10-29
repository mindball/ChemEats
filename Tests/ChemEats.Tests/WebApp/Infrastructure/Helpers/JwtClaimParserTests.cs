using System.Security.Claims;
using Domain.Infrastructure.Identity;
using WebApi.Infrastructure.Identity;
using WebApp.Infrastructure.Helpers;

namespace ChemEats.Tests.WebApp.Infrastructure.Helpers;

public class JwtClaimParserTests
{
    [Fact]
    public void ParseClaims_WhenTokenHasSingleRole_ShouldReturnRoleClaim()
    {
        string token = CreateToken(["Admin"]);

        ClaimsPrincipal principal = CreatePrincipal(token);

        Assert.True(principal.IsInRole("Admin"));
    }

    [Fact]
    public void ParseClaims_WhenTokenHasMultipleRoles_ShouldReturnAllRoleClaims()
    {
        string token = CreateToken(["Admin", "Supervisor"]);

        ClaimsPrincipal principal = CreatePrincipal(token);

        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("Supervisor"));
    }

    private static ClaimsPrincipal CreatePrincipal(string token)
    {
        List<Claim> claims = JwtClaimParser.ParseClaims(token).ToList();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
    }

    private static string CreateToken(IList<string> roles)
    {
        JwtTokenProvider provider = new(new JwtSettings(
            "ChemEats",
            "ChemEatsClient",
            "super-secret-test-key-with-at-least-32-chars",
            60));

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "MM",
            Email = "mm@cpachem.com"
        };

        return provider.GenerateToken(user, roles);
    }
}
