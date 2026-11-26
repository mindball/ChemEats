using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using ChemEats.Tests.TestInfrastructure;
using WebApp.Components;
using WebApp.Infrastructure.Localization;

namespace ChemEats.Tests.WebApp.Components;

public class LanguageSelectorTests : LocalizedTestContext
{
    [Fact]
    public void LanguageSelector_WhenEnglishIsClicked_ShouldStoreLanguage()
    {
        JSInterop.SetupVoid("localStorage.setItem", AppCulture.StorageKey, AppCulture.English).SetVoidResult();
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("bg-BG");
            IRenderedComponent<LanguageSelector> component = RenderComponent<LanguageSelector>();

            IElement englishButton = component.FindAll("button").Single(button => button.TextContent == "EN");
            englishButton.Click();

            JSInterop.VerifyInvoke("localStorage.setItem");
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void LanguageSelector_WhenCurrentLanguageIsClicked_ShouldNotStoreAnything()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("bg-BG");
            IRenderedComponent<LanguageSelector> component = RenderComponent<LanguageSelector>();

            IElement bulgarianButton = component.FindAll("button").Single(button => button.TextContent == "БГ");
            Assert.Contains("btn-primary", bulgarianButton.ClassList);

            bulgarianButton.Click();

            Assert.Empty(JSInterop.Invocations);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
