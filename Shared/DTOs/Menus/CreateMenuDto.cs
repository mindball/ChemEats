using Shared.DTOs.Meals;

namespace Shared.DTOs.Menus;

public record CreateMenuDto(
    Guid SupplierId,
    DateTime Date,
    DateTime ActiveUntil,
    List<CreateMealDto> Meals
);