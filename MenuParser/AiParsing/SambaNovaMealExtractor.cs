using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

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
