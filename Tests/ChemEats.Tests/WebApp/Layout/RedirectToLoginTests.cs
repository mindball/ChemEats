using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Layout;

namespace ChemEats.Tests.WebApp.Layout;

public class RedirectToLoginTests : TestContext
{
    [Fact]
    public void RedirectToLogin_OnInitialized_ShouldNavigateToLoginPath()
    {
        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        RenderComponent<RedirectToLogin>();

        Assert.Contains("Account/Login", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
