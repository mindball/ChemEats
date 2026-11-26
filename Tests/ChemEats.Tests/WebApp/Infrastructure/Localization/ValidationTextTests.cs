using System.Globalization;
using WebApp.Infrastructure.Localization;

namespace ChemEats.Tests.WebApp.Infrastructure.Localization;

public class ValidationTextTests
{
    [Fact]
    public void Required_WhenUiCultureIsBulgarian_ShouldReturnBulgarianMessage()
    {
        string message = WithUiCulture("bg-BG", () => ValidationText.Required);

        Assert.Equal("Полето „{0}“ е задължително.", message);
    }

    [Fact]
    public void Required_WhenUiCultureIsEnglish_ShouldReturnEnglishMessage()
    {
        string message = WithUiCulture("en-US", () => ValidationText.Required);

        Assert.Equal("The {0} field is required.", message);
    }

    [Fact]
    public void PasswordsDoNotMatch_WhenUiCultureIsBulgarian_ShouldReturnBulgarianMessage()
    {
        string message = WithUiCulture("bg-BG", () => ValidationText.PasswordsDoNotMatch);

        Assert.Equal("Паролите не съвпадат", message);
    }

    private static string WithUiCulture(string cultureName, Func<string> read)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
