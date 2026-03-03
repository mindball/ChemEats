using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

public class GeminiMealExtractor : IAiMealExtractor
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiMealExtractor> _logger;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiMealExtractor(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiMealExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(
        string textContent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Gemini API key is not configured. Set 'Gemini:ApiKey' in appsettings or user secrets.");

        string prompt = BuildPrompt(textContent);
        string url = $"{BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

        GeminiRequest request = new(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig("application/json")
        );

        _logger.LogInformation("Sending menu text to Gemini AI for parsing ({Length} characters)", textContent.Length);

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned {StatusCode}: {Error}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Gemini AI error ({response.StatusCode}). Please try again.");
        }

        GeminiResponse? geminiResponse = await response.Content
            .ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);

        string? jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            _logger.LogWarning("Gemini returned empty response");
            return [];
        }

        _logger.LogDebug("Gemini raw response: {Response}", jsonText);

        List<ParsedMeal> meals = DeserializeMeals(jsonText);

        List<ParsedMeal> validMeals = meals
            .Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Price > 0)
            .ToList();

        _logger.LogInformation("Parsed {Count} valid meals from menu", validMeals.Count);
        return validMeals;
    }

    private List<ParsedMeal> DeserializeMeals(string jsonText)
    {
        try
        {
            List<ParsedMeal>? meals = JsonSerializer.Deserialize<List<ParsedMeal>>(jsonText, JsonOptions);
            if (meals is not null)
                return meals;
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to deserialize as array, trying wrapped format");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonText);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        List<ParsedMeal>? meals = property.Value.Deserialize<List<ParsedMeal>>(JsonOptions);
                        if (meals is not null)
                            return meals;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response JSON: {Json}", jsonText);
        }

        return [];
    }

    private static string BuildPrompt(string textContent) => $$"""
        You are a menu parser for a food ordering system. Extract all meals/dishes and their prices from the following menu content.

        Rules:
        - Extract every meal/dish with its price
        - Return prices as decimal numbers (no currency symbols, no currency codes)
        - If prices use comma as decimal separator, convert to dot notation
        - Ignore headers, footers, supplier names, dates, IDs, GUIDs, or non-meal content
        - If a line has no clear price, skip it
        - Preserve the original meal name language (could be Bulgarian, English, etc.)
        - Prices might be in EUR, BGN, лв, or other currencies - just extract the numeric value
        - Files may be CSV (semicolon or comma delimited), Excel tables, or Word documents

        Return a JSON array where each element has exactly two fields:
        - "name": the meal/dish name (string, trimmed)
        - "price": the price as a number (decimal, e.g. 5.50)

        Example output: [{"name": "Пилешка супа", "price": 3.50}, {"name": "Салата Цезар", "price": 4.20}]

        Menu content:
        ---
        {{textContent}}
        ---
        """;


    private record GeminiRequest(
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig
    );

    private record GeminiContent(
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts
    );

    private record GeminiPart(
        [property: JsonPropertyName("text")] string Text
    );

    private record GeminiGenerationConfig(
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType
    );

    private record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates
    );

    private record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content
    );
}
