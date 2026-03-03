using System.Net;
using MenuParser.Abstractions;
using MenuParser.AiParsing;
using MenuParser.TextExtraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace MenuParser.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMenuParser(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));

        services.AddSingleton<ITextExtractor, CsvTextExtractor>();
        services.AddSingleton<ITextExtractor, ExcelTextExtractor>();
        services.AddSingleton<ITextExtractor, WordTextExtractor>();
        services.AddSingleton<TextExtractorFactory>();

        services.AddHttpClient<IAiMealExtractor, GeminiMealExtractor>(client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("gemini-pipeline", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(30),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                });

                builder.AddTimeout(TimeSpan.FromMinutes(5));
            });

        services.AddScoped<IMenuFileParser, MenuFileParser>();

        return services;
    }
}
