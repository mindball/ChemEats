namespace Shared.DTOs.Meals;

public record CreateMealDto(
    string Name,
    decimal Price
);