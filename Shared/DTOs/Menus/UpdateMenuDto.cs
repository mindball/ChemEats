using Shared.DTOs.Meals;

namespace Shared.DTOs.Menus;

public record UpdateMenuDto(
    Guid Id,
    Guid SupplierId,
    DateTime Date,
    List<MealDto> Meals
);