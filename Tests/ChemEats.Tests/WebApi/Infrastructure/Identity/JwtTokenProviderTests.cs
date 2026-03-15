using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Infrastructure.Identity;
using WebApi.Infrastructure.Identity;

namespace ChemEats.Tests.WebApi.Infrastructure.Identity;

public class JwtTokenProviderTests
{
    [Fact]
    public void GenerateToken_WhenUserIsNull_ShouldThrow()
    {
        JwtTokenProvider provider = CreateProvider();

        Assert.Throws<ArgumentNullException>(() => provider.GenerateToken(null!, []));
    }

    [Fact]
    public void GenerateToken_WhenUserNameOrEmailMissing_ShouldThrow()
    {
        JwtTokenProvider provider = CreateProvider();
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = null,
            Email = "mail@cpachem.com"
        };

        Assert.Throws<ArgumentException>(() => provider.GenerateToken(user, []));
    }

    [Fact]
    public void GenerateToken_ShouldContainExpectedClaims()
    {
        JwtTokenProvider provider = CreateProvider();
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "MM",
            Email = "mm@cpachem.com"
        };

        string token = provider.GenerateToken(user, ["Admin", "Employee"]);

        JwtSecurityTokenHandler tokenHandler = new();
        JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);

        Assert.Equal("issuer", jwt.Issuer);
        Assert.Equal("audience", jwt.Audiences.Single());
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id);
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Name && claim.Value == "MM");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == "mm@cpachem.com");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Employee");
    }

    private static JwtTokenProvider CreateProvider()
    {
        JwtSettings settings = new("issuer", "audience", "super_secret_key_that_is_long_enough_123", 60);
        return new JwtTokenProvider(settings);
    }
}
