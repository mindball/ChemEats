using System.Globalization;
using System.Resources;
using WebApp.Resources;

namespace WebApp.Infrastructure.Localization;

/// <summary>
/// Static accessors for DataAnnotations (ErrorMessageResourceType / Display.ResourceType),
/// which cannot use an injected IStringLocalizer.
/// </summary>
public static class ValidationText
{
    private static readonly ResourceManager Resources =
        new(typeof(SharedResource).FullName!, typeof(SharedResource).Assembly);

    public static string Required => Get("The {0} field is required.");
    public static string PasswordsDoNotMatch => Get("Passwords do not match");
    public static string CurrentPassword => Get("Current password");
    public static string NewPassword => Get("New password");
    public static string ConfirmNewPassword => Get("Confirm new password");
    public static string Username => Get("Username");
    public static string Password => Get("Password");
    public static string RememberMe => Get("Remember me");

    private static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
