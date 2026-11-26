using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using WebApp.Resources;

namespace ChemEats.Tests.WebApp.Resources;

public class SharedResourceTests
{
    private static readonly ResourceManager Resources =
        new(typeof(SharedResource).FullName!, typeof(SharedResource).Assembly);

    private static readonly CultureInfo Bulgarian = new("bg-BG");
    private static readonly CultureInfo BulgarianNeutral = new("bg");

    [Fact]
    public void NeutralResource_ShouldUseEnglishSourceTextAsKeyAndValue()
    {
        Dictionary<string, string> neutral = LoadSet(CultureInfo.InvariantCulture);

        Assert.NotEmpty(neutral);
        Assert.All(neutral, entry => Assert.Equal(entry.Key, entry.Value));
    }

    [Fact]
    public void NeutralResource_ShouldNotHaveKeysDifferingOnlyByCase()
    {
        Dictionary<string, string> neutral = LoadSet(CultureInfo.InvariantCulture);

        List<string> duplicates = neutral.Keys
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void BulgarianResource_ShouldTranslateEveryNeutralKey()
    {
        Dictionary<string, string> neutral = LoadSet(CultureInfo.InvariantCulture);
        Dictionary<string, string> bulgarian = LoadSet(BulgarianNeutral);

        List<string> missing = neutral.Keys.Where(key => !bulgarian.ContainsKey(key) || string.IsNullOrWhiteSpace(bulgarian[key])).ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void BulgarianResource_ShouldKeepTheSameFormatPlaceholders()
    {
        Dictionary<string, string> bulgarian = LoadSet(BulgarianNeutral);

        List<string> mismatched = bulgarian
            .Where(entry => !Placeholders(entry.Key).SetEquals(Placeholders(entry.Value)))
            .Select(entry => entry.Key)
            .ToList();

        Assert.Empty(mismatched);
    }

    [Fact]
    public void GetString_WhenUiCultureIsBulgarian_ShouldReturnBulgarianText()
    {
        string? text = Resources.GetString("My Orders", Bulgarian);

        Assert.Equal("Моите поръчки", text);
    }

    [Fact]
    public void GetString_WhenUiCultureIsEnglish_ShouldReturnEnglishText()
    {
        string? text = Resources.GetString("My Orders", new CultureInfo("en-US"));

        Assert.Equal("My Orders", text);
    }

    private static Dictionary<string, string> LoadSet(CultureInfo culture)
    {
        ResourceSet set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
                          ?? throw new InvalidOperationException($"Resource set for '{culture.Name}' not found.");

        return set.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }

    private static HashSet<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+(:[^}]*)?\}").Select(match => match.Value).ToHashSet();
}
