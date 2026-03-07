using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

public class GroqMealExtractor : OpenAiCompatibleExtractorBase
{
    protected override string BaseUrl => "https://api.groq.com/openai/v1/chat/completions";
    protected override string ProviderName => "Groq";

    public GroqMealExtractor(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        ILogger<GroqMealExtractor> logger)
        : base(httpClient, options.Value.ApiKey, options.Value.Model, options.Value.MaxRetries, logger)
    {
    }
}
