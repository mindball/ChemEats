namespace WebApi.Infrastructure.Employees;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Abbreviation { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }

    // Temporary password for the first login; the admin is forced to change it.
    public string? Password { get; set; }
}
