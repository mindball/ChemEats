using MenuParser.Models;

namespace MenuParser.Abstractions;

public interface IAiMealExtractor
{
    Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(string textContent, CancellationToken cancellationToken = default);
}
