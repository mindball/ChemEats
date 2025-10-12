namespace Shared.DTOs.Meals;

public record MealDto(
    Guid Id,      
    string Name,
    decimal Price
);