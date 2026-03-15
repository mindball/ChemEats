using Bunit;
using Bunit.TestDoubles;
using AngleSharp.Dom;
using WebApp.Layout;

namespace ChemEats.Tests.WebApp.Layout;

public class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_WhenTogglerIsClicked_ShouldExpandAndCollapseMenu()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("basic-user");

        IRenderedComponent<NavMenu> component = RenderComponent<NavMenu>();

        IElement navContainer = component.Find("div.nav-scrollable");
        Assert.Contains("collapse", navContainer.ClassName, StringComparison.Ordinal);

        IElement toggleButton = component.Find("button.navbar-toggler");
        toggleButton.Click();

        navContainer = component.Find("div.nav-scrollable");
        Assert.DoesNotContain("collapse", navContainer.ClassName, StringComparison.Ordinal);

        toggleButton.Click();

        navContainer = component.Find("div.nav-scrollable");
        Assert.Contains("collapse", navContainer.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void NavMenu_WhenUserIsAnonymous_ShouldRenderOnlyPublicLink()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetNotAuthorized();

        IRenderedComponent<NavMenu> component = RenderComponent<NavMenu>();

        Assert.Contains("Dashboard", component.Markup);
        Assert.DoesNotContain("Place Order", component.Markup);
        Assert.DoesNotContain("Create Menu", component.Markup);
        Assert.DoesNotContain("Add Supplier", component.Markup);
    }

    [Fact]
    public void NavMenu_WhenUserIsAdmin_ShouldRenderAdministrationSections()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("admin-user");
        authorizationContext.SetRoles("Admin");

        IRenderedComponent<NavMenu> component = RenderComponent<NavMenu>();

        Assert.Contains("Browse Menus", component.Markup);
        Assert.Contains("Place Order", component.Markup);
        Assert.Contains("Create Menu", component.Markup);
        Assert.Contains("Add Supplier", component.Markup);
        Assert.Contains("Control Panel", component.Markup);
        Assert.Contains("Payments", component.Markup);
    }
}
