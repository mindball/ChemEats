namespace Shared.DTOs.Meals;

public record MealDto(
    Guid Id,      
    Guid MenuId,
    string Name,
    decimal Price
);