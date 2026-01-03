using Domain.Entities;
using Shared.DTOs.Meals;

namespace Services.Factories.Meals;

public static class MealFactory
{
    public static Meal FromDto(CreateMealDto dto, Guid menuId)
        => Meal.Create(menuId, dto.Name, new Price(dto.Price));

    public static Meal FromMapping(MealDto dto)
        => Meal.Create(dto.MenuId, dto.Name, new Price(dto.Price));
}