using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;

namespace MenuParser.AiParsing;

public class FallbackAiMealExtractor : IAiMealExtractor
{
    private readonly OllamaMealExtractor _primary;
    private readonly GroqMealExtractor _firstFallback;
    private readonly SambaNovaMealExtractor _secondFallback;
    private readonly ILogger<FallbackAiMealExtractor> _logger;

    public FallbackAiMealExtractor(
        OllamaMealExtractor primary,
        GroqMealExtractor firstFallback,
        SambaNovaMealExtractor secondFallback,
        ILogger<FallbackAiMealExtractor> logger)
    {
        _primary = primary;
        _firstFallback = firstFallback;
        _secondFallback = secondFallback;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(
        string textContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.ExtractMealsAsync(textContent, cancellationToken);
        }
        catch (Exception primaryException)
        {
            _logger.LogWarning(primaryException,
                "Ollama extraction failed, activating Groq fallback");

            try
            {
                return await _firstFallback.ExtractMealsAsync(textContent, cancellationToken);
            }
            catch (Exception firstFallbackException)
            {
                _logger.LogWarning(firstFallbackException,
                    "Groq extraction failed, activating SambaNova fallback");

                return await _secondFallback.ExtractMealsAsync(textContent, cancellationToken);
            }
        }
    }
}
