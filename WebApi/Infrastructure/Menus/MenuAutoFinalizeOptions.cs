namespace WebApi.Infrastructure.Menus;

public sealed class MenuAutoFinalizeOptions
{
    public const string SectionName = "MenuAutoFinalize";

    public bool Enabled { get; set; } = true;

    // How often the background job looks for expired menus that still have pending orders.
    public int CheckIntervalMinutes { get; set; } = 5;
}
