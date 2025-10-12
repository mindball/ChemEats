using Microsoft.AspNetCore.Components;
using Shared.DTOs.Menus;

namespace WebApp.Pages.Menus;

public class MenuListComponentBase : ComponentBase
{
    [Parameter]
    public IReadOnlyList<MenuDto>? Menus { get; set; }
}