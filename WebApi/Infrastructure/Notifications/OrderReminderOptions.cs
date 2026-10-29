namespace WebApi.Infrastructure.Notifications;

public sealed class OrderReminderOptions
{
    public const string SectionName = "OrderReminders";

    public bool Enabled { get; set; }

    // How often the background job looks for menus that are about to close.
    public int CheckIntervalMinutes { get; set; } = 5;

    // A reminder is sent once the menu's ActiveUntil is within this many minutes.
    public int LeadTimeMinutes { get; set; } = 120;

    // Optional link to the ordering page included in the email.
    public string? ApplicationUrl { get; set; }
}
