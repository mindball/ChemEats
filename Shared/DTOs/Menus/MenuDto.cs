using Shared.DTOs.Meals;

namespace Shared.DTOs.Menus;

public record MenuDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime Date,
    List<MealDto> Meals,
    bool IsDeleted
);