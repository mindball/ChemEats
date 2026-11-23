using System.Text.Json.Serialization;

namespace Shared.DTOs.Employees;

[Obsolete("External HR employee integration is disabled: the isolated host has no access to the HR service. Employees are created manually by an admin. Kept for a possible future integration.")]
public class UserDto
{
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;  // идва от външния API, ще стане Abbreviation

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty; 

    // [JsonPropertyName("IsActive")]
    // public bool IsActive { get; set; }
}