using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

public class GeminiMealExtractor : OpenAiCompatibleExtractorBase
{
    // Google's OpenAI-compatible endpoint, so the shared base class applies unchanged.
    protected override string BaseUrl => "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    protected override string ProviderName => "Gemini";

    public GeminiMealExtractor(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiMealExtractor> logger)
        : base(httpClient, options.Value.ApiKey, options.Value.Model, options.Value.MaxRetries, logger)
    {
    }
}
