using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

[Obsolete("SambaNova requires a paid plan (API returns 402 PAYMENT_METHOD_REQUIRED) and is no longer registered or used. Kept for reference; use GeminiMealExtractor instead.")]
public class SambaNovaMealExtractor : OpenAiCompatibleExtractorBase
{
    protected override string BaseUrl => "https://api.sambanova.ai/v1/chat/completions";
    protected override string ProviderName => "SambaNova";

    public SambaNovaMealExtractor(
        HttpClient httpClient,
        IOptions<SambaNovaOptions> options,
        ILogger<SambaNovaMealExtractor> logger)
        : base(httpClient, options.Value.ApiKey, options.Value.Model, options.Value.MaxRetries, logger)
    {
    }
}
