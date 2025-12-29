using System.Security.Claims;
using System.Text.Json;

namespace WebApp.Infrastructure.Helpers;

public static class JwtClaimParser
{
    public static IEnumerable<Claim> ParseClaims(string jwt)
    {
        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
            yield break;

        string payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        byte[] jsonBytes;
        try
        {
            jsonBytes = Convert.FromBase64String(payload);
        }
        catch
        {
            yield break;
        }

        using JsonDocument doc = JsonDocument.Parse(jsonBytes);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("unique_name", out JsonElement uniqueName))
            yield return new Claim(ClaimTypes.Name, uniqueName.GetString() ?? string.Empty);

        if (root.TryGetProperty("name", out JsonElement name))
            yield return new Claim(ClaimTypes.Name, name.GetString() ?? string.Empty);

        if (root.TryGetProperty("sub", out JsonElement sub))
            yield return new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? string.Empty);

        if (root.TryGetProperty("email", out JsonElement email))
            yield return new Claim(ClaimTypes.Email, email.GetString() ?? string.Empty);

        if (root.TryGetProperty("role", out JsonElement roleProp))
        {
            if (roleProp.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement r in roleProp.EnumerateArray())
                    if (r.ValueKind == JsonValueKind.String)
                        yield return new Claim(ClaimTypes.Role, r.GetString() ?? string.Empty);
            }
            else if (roleProp.ValueKind == JsonValueKind.String)
            {
                yield return new Claim(ClaimTypes.Role, roleProp.GetString() ?? string.Empty);
            }
        }
        else if (root.TryGetProperty("roles", out JsonElement rolesProp))
        {
            if (rolesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement r in rolesProp.EnumerateArray())
                    if (r.ValueKind == JsonValueKind.String)
                        yield return new Claim(ClaimTypes.Role, r.GetString() ?? string.Empty);
            }
            else if (rolesProp.ValueKind == JsonValueKind.String)
            {
                yield return new Claim(ClaimTypes.Role, rolesProp.GetString() ?? string.Empty);
            }
        }

        foreach (JsonProperty property in root.EnumerateObject())
        {
            string nameKey = property.Name;
            if (nameKey == "unique_name" || nameKey == "name" || nameKey == "sub" ||
                nameKey == "email" || nameKey == "role" || nameKey == "roles")
                continue;

            JsonElement el = property.Value;
            if (el.ValueKind == JsonValueKind.String)
                yield return new Claim(nameKey, el.GetString() ?? string.Empty);
        }
    }
}