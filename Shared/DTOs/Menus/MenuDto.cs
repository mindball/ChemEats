using Shared.DTOs.Meals;

namespace Shared.DTOs.Menus;

public record MenuDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    DateTime ActiveUntil,
    bool IsActive,
    bool IsDeleted,
    List<MealDto> Meals
);