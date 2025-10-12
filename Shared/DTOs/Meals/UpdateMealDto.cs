namespace Shared.DTOs.Meals;

public record UpdateMealDto(
    Guid Id,      
    string Name,
    decimal Price
);