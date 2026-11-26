using System.Globalization;
using Microsoft.JSInterop;

namespace WebApp.Infrastructure.Localization;

/// <summary>
/// UI language handling. Number/date formatting always stays bg-BG with the euro symbol;
/// only the UI culture (which .resx is used) follows the user's language choice.
/// </summary>
public static class AppCulture
{
    public const string Bulgarian = "bg";
    public const string English = "en";
    public const string StorageKey = "chemeats.lang";

    public static string CurrentLanguage =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == English ? English : Bulgarian;

    public static async Task ApplyAsync(IJSRuntime jsRuntime)
    {
        string? language = null;
        try
        {
            language = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch (JSException)
        {
            // Storage unavailable (private mode etc.) - fall back to the default language.
        }

        (CultureInfo formatCulture, CultureInfo uiCulture) = Resolve(language);

        CultureInfo.DefaultThreadCurrentCulture = formatCulture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.CurrentCulture = formatCulture;
        CultureInfo.CurrentUICulture = uiCulture;
    }

    public static (CultureInfo FormatCulture, CultureInfo UiCulture) Resolve(string? language)
    {
        CultureInfo formatCulture = CreateFormatCulture();
        CultureInfo uiCulture = language == English ? new CultureInfo("en-US") : formatCulture;
        return (formatCulture, uiCulture);
    }

    public static async Task SetLanguageAsync(IJSRuntime jsRuntime, string language)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, language);
    }

    private static CultureInfo CreateFormatCulture()
    {
        CultureInfo culture = (CultureInfo)new CultureInfo("bg-BG").Clone();
        culture.NumberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        culture.NumberFormat.CurrencySymbol = "€";
        return culture;
    }
}
