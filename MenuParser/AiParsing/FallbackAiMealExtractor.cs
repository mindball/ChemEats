using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;

namespace MenuParser.AiParsing;

public class FallbackAiMealExtractor : IAiMealExtractor
{
    private static readonly TimeSpan PrimaryTimeout = TimeSpan.FromMinutes(1);

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
        using CancellationTokenSource timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(PrimaryTimeout);

        try
        {
            IReadOnlyList<ParsedMeal> primaryMeals = await _primary.ExtractMealsAsync(textContent, timeoutCancellationTokenSource.Token);

            if (!cancellationToken.IsCancellationRequested && !timeoutCancellationTokenSource.IsCancellationRequested)
            {
                return primaryMeals;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            _logger.LogWarning("Ollama extraction exceeded {Timeout} and fallback extraction will be used.", PrimaryTimeout);
            return await ExtractFromFallbacksAsync(textContent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception primaryException)
        {
            _logger.LogWarning(primaryException,
                "Ollama extraction failed, activating Groq fallback");

            return await ExtractFromFallbacksAsync(textContent, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ParsedMeal>> ExtractFromFallbacksAsync(
        string textContent,
        CancellationToken cancellationToken)
    {
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
