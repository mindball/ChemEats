namespace WebApi.Infrastructure.Employees;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Abbreviation { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}
