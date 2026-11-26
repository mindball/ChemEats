using System.Globalization;
using Microsoft.Extensions.Localization;

namespace ChemEats.Tests.TestInfrastructure;

/// <summary>
/// Deterministic localizer for UI tests: returns the English resource key (formatted),
/// independent of the machine's UI culture.
/// </summary>
public sealed class PassThroughStringLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(CultureInfo.CurrentCulture, name, arguments), resourceNotFound: false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
