using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using WebApp.Resources;

namespace WebApp.Infrastructure.Localization;

public abstract class LocalizedComponentBase : ComponentBase
{
    [Inject] protected IStringLocalizer<SharedResource> L { get; set; } = null!;
}
