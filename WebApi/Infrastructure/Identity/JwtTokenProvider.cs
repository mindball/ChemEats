using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Infrastructure.Identity;

public class JwtTokenProvider
{
    private readonly JwtSettings _settings;

    public JwtTokenProvider(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _settings.Issuer,
            _settings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record JwtSettings(string Issuer, string Audience, string Secret, int ExpiryMinutes);