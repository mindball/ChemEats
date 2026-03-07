using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;

namespace MenuParser.AiParsing;

public class FallbackAiMealExtractor : IAiMealExtractor
{
    private static readonly ResiliencePropertyKey<string> TextContentKey = new("textContent");

    private readonly GroqMealExtractor _primary;
    private readonly SambaNovaMealExtractor _fallback;
    private readonly ILogger<FallbackAiMealExtractor> _logger;
    private readonly ResiliencePipeline<IReadOnlyList<ParsedMeal>> _pipeline;

    public FallbackAiMealExtractor(
        GroqMealExtractor primary,
        SambaNovaMealExtractor fallback,
        ILogger<FallbackAiMealExtractor> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
        _pipeline = BuildPipeline();
    }

    public async Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(
        string textContent,
        CancellationToken cancellationToken = default)
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get(cancellationToken);
        try
        {
            context.Properties.Set(TextContentKey, textContent);
            return await _pipeline.ExecuteAsync(
                async ctx => await _primary.ExtractMealsAsync(textContent, ctx.CancellationToken),
                context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private ResiliencePipeline<IReadOnlyList<ParsedMeal>> BuildPipeline() =>
        new ResiliencePipelineBuilder<IReadOnlyList<ParsedMeal>>()
            .AddFallback(new FallbackStrategyOptions<IReadOnlyList<ParsedMeal>>
            {
                ShouldHandle = new PredicateBuilder<IReadOnlyList<ParsedMeal>>().Handle<Exception>(),
                OnFallback = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception,
                        "Groq extraction failed, activating SambaNova fallback");
                    return ValueTask.CompletedTask;
                },
                FallbackAction = async args =>
                {
                    args.Context.Properties.TryGetValue(TextContentKey, out string? textContent);
                    IReadOnlyList<ParsedMeal> result = await _fallback.ExtractMealsAsync(
                        textContent ?? string.Empty,
                        args.Context.CancellationToken);
                    return Outcome.FromResult<IReadOnlyList<ParsedMeal>>(result);
                }
            })
            .Build();
}
