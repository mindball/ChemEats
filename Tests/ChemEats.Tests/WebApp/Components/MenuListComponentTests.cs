using Bunit;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using WebApp.Components;

namespace ChemEats.Tests.WebApp.Components;

public class MenuListComponentTests : TestContext
{
    [Fact]
    public void MenuListComponent_WhenMenusAreNull_ShouldRenderEmptyState()
    {
        IRenderedComponent<MenuListComponent> component = RenderComponent<MenuListComponent>(
            parameters => parameters.Add(parameter => parameter.Menus, null));

        Assert.Contains("No menus found.", component.Markup);
    }

    [Fact]
    public void MenuListComponent_WhenMenusHaveItems_ShouldRenderRowsAndMeals()
    {
        List<MenuDto> menus =
        [
            new MenuDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Supplier One",
                DateTime.Today.AddDays(1),
                DateTime.Today.AddDays(1).AddHours(12),
                true,
                false,
                [new MealDto(Guid.NewGuid(), Guid.NewGuid(), "Soup", 10m), new MealDto(Guid.NewGuid(), Guid.NewGuid(), "Salad", 8m)])
        ];

        IRenderedComponent<MenuListComponent> component = RenderComponent<MenuListComponent>(
            parameters => parameters.Add(parameter => parameter.Menus, menus));

        Assert.Contains("Supplier One", component.Markup);
        Assert.Contains("Soup", component.Markup);
        Assert.Contains("Salad", component.Markup);
    }
}
