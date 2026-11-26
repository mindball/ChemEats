using System.Globalization;
using WebApp.Infrastructure.Localization;

namespace ChemEats.Tests.WebApp.Infrastructure.Localization;

public class AppCultureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("bg")]
    [InlineData("unknown")]
    public void Resolve_WhenLanguageIsNotEnglish_ShouldUseBulgarianUi(string? language)
    {
        (CultureInfo formatCulture, CultureInfo uiCulture) = AppCulture.Resolve(language);

        Assert.Equal("bg-BG", uiCulture.Name);
        Assert.Equal("bg-BG", formatCulture.Name);
    }

    [Fact]
    public void Resolve_WhenLanguageIsEnglish_ShouldUseEnglishUiButKeepBulgarianFormatting()
    {
        (CultureInfo formatCulture, CultureInfo uiCulture) = AppCulture.Resolve("en");

        Assert.Equal("en-US", uiCulture.Name);
        Assert.Equal("bg-BG", formatCulture.Name);
    }

    [Fact]
    public void Resolve_ShouldFormatCurrencyInEuro()
    {
        (CultureInfo formatCulture, _) = AppCulture.Resolve("en");

        Assert.Equal("€", formatCulture.NumberFormat.CurrencySymbol);
    }
}
