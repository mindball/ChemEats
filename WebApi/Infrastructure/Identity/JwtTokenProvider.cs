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

    public string GenerateToken(ApplicationUser user, IList<string> roles, bool mustChangePassword = false)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.UserName is null)
        {
            throw new ArgumentException("User must have a non-null UserName.", nameof(user));
        }

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName)
        ];

        // Manually created employees may have no email.
        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (mustChangePassword)
            claims.Add(new Claim(PasswordChangeRequirement.ClaimType, PasswordChangeRequirement.ClaimValue));

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.Secret));
        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            _settings.Issuer,
            _settings.Audience,
            claims,
            expires: DateTime.Now.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: creds);

        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }
}

public record JwtSettings(string Issuer, string Audience, string Secret, int ExpiryMinutes);