using MenuParser.Abstractions;
using MenuParser.AiParsing;
using MenuParser.TextExtraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMenuParser(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));

        services.AddSingleton<ITextExtractor, CsvTextExtractor>();
        services.AddSingleton<ITextExtractor, ExcelTextExtractor>();
        services.AddSingleton<ITextExtractor, WordTextExtractor>();
        services.AddSingleton<TextExtractorFactory>();

        services.AddHttpClient("ollama", (serviceProvider, client) =>
        {
            OllamaOptions ollamaOptions = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            int timeoutSeconds = Math.Max(61, ollamaOptions.HttpTimeoutSeconds);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });
        services.AddHttpClient("groq", client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient("gemini", client => client.Timeout = TimeSpan.FromSeconds(60));

        services.AddSingleton<OllamaMealExtractor>(sp => new OllamaMealExtractor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama"),
            sp.GetRequiredService<IOptions<OllamaOptions>>(),
            sp.GetRequiredService<ILogger<OllamaMealExtractor>>()));

        services.AddSingleton<GroqMealExtractor>(sp => new GroqMealExtractor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("groq"),
            sp.GetRequiredService<IOptions<GroqOptions>>(),
            sp.GetRequiredService<ILogger<GroqMealExtractor>>()));

        services.AddSingleton<GeminiMealExtractor>(sp => new GeminiMealExtractor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("gemini"),
            sp.GetRequiredService<IOptions<GeminiOptions>>(),
            sp.GetRequiredService<ILogger<GeminiMealExtractor>>()));

        services.AddSingleton<IAiMealExtractor, FallbackAiMealExtractor>();

        services.AddScoped<IMenuFileParser, MenuFileParser>();

        return services;
    }
}
