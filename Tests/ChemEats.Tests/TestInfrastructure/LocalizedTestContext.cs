using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using WebApp.Resources;

namespace ChemEats.Tests.TestInfrastructure;

/// <summary>
/// bunit context with the UI string localizer registered (all WebApp components inject IStringLocalizer).
/// </summary>
public abstract class LocalizedTestContext : TestContext
{
    protected LocalizedTestContext()
    {
        Services.AddSingleton<IStringLocalizer<SharedResource>>(new PassThroughStringLocalizer<SharedResource>());
    }
}
