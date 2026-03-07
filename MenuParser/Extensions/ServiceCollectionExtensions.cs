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
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.Configure<SambaNovaOptions>(configuration.GetSection("SambaNova"));

        services.AddSingleton<ITextExtractor, CsvTextExtractor>();
        services.AddSingleton<ITextExtractor, ExcelTextExtractor>();
        services.AddSingleton<ITextExtractor, WordTextExtractor>();
        services.AddSingleton<TextExtractorFactory>();

        services.AddHttpClient("groq", client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient("sambanova", client => client.Timeout = TimeSpan.FromSeconds(60));

        services.AddSingleton<GroqMealExtractor>(sp => new GroqMealExtractor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("groq"),
            sp.GetRequiredService<IOptions<GroqOptions>>(),
            sp.GetRequiredService<ILogger<GroqMealExtractor>>()));

        services.AddSingleton<SambaNovaMealExtractor>(sp => new SambaNovaMealExtractor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("sambanova"),
            sp.GetRequiredService<IOptions<SambaNovaOptions>>(),
            sp.GetRequiredService<ILogger<SambaNovaMealExtractor>>()));

        services.AddSingleton<IAiMealExtractor, FallbackAiMealExtractor>();

        services.AddScoped<IMenuFileParser, MenuFileParser>();

        return services;
    }
}
