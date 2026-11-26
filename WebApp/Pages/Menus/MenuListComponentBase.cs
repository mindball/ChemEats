using Microsoft.AspNetCore.Components;
using Shared.DTOs.Menus;
using WebApp.Infrastructure.Localization;

namespace WebApp.Pages.Menus;

public class MenuListComponentBase : LocalizedComponentBase
{
    [Parameter]
    public IReadOnlyList<MenuDto>? Menus { get; set; }
}