using System.Text.Json.Serialization;

namespace Shared.DTOs.Employees;

public class UserDto
{
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;  // идва от външния API, ще стане Abbreviation

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty; 

    // [JsonPropertyName("IsActive")]
    // public bool IsActive { get; set; }
}