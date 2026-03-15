using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;

namespace MenuParser.AiParsing;

public abstract class OpenAiCompatibleExtractorBase : IAiMealExtractor
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxRetries;
    private readonly ILogger _logger;

    protected abstract string BaseUrl { get; }
    protected abstract string ProviderName { get; }
    protected virtual bool RequiresApiKey => true;
    protected virtual bool SupportsResponseFormat => true;
    protected virtual bool UseSeparateSystemMessage => false;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected OpenAiCompatibleExtractorBase(
        HttpClient httpClient,
        string apiKey,
        string model,
        int maxRetries,
        ILogger logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;
        _maxRetries = maxRetries;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(
        string textContent,
        CancellationToken cancellationToken = default)
    {
        if (RequiresApiKey && string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                $"{ProviderName} API key is not configured.");

        List<ChatMessage> messages = UseSeparateSystemMessage
            ? BuildMessagesWithSystem(textContent)
            : BuildMessagesWithUserOnly(textContent);

        ChatRequest request = new(
            Model: _model,
            Messages: messages,
            Temperature: 0.0m,
            ResponseFormat: SupportsResponseFormat ? new ResponseFormat("json_object") : null
        );

        _logger.LogInformation("Sending menu text to {Provider} ({Model}, {Length} chars)",
            ProviderName, _model, textContent.Length);

        HttpResponseMessage response = await SendWithRetryAsync(request, cancellationToken);

        ChatResponse? chatResponse = await response.Content
            .ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);

        string? jsonText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            _logger.LogWarning("{Provider} returned empty response", ProviderName);
            return [];
        }

        _logger.LogDebug("{Provider} raw response: {Response}", ProviderName, jsonText);

        List<ParsedMeal> meals = DeserializeMeals(jsonText);

        List<ParsedMeal> validMeals = meals
            .Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Price > 0)
            .ToList();

        _logger.LogInformation("Successfully parsed {Count} meals from menu via {Provider}",
            validMeals.Count, ProviderName);

        return validMeals;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Clamp(_maxRetries, 1, 3);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            _logger.LogInformation("{Provider} API request attempt {Attempt}/{Max}",
                ProviderName, attempt, maxAttempts);

            using HttpRequestMessage httpRequest = new(HttpMethod.Post, BaseUrl);
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                httpRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }

            httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

            HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("{Provider} API responded successfully", ProviderName);
                return httpResponse;
            }

            string errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("{Provider} API returned {StatusCode}: {Error}",
                ProviderName, httpResponse.StatusCode, errorBody);

            if (httpResponse.StatusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(
                    "{Provider} internal error (500) - model may not support the request format. Error: {Error}",
                    ProviderName, errorBody);
                throw new InvalidOperationException(
                    $"{ProviderName} internal error: {errorBody}");
            }

            bool isRateLimited = httpResponse.StatusCode == HttpStatusCode.TooManyRequests;

            if (!isRateLimited)
                throw new InvalidOperationException(
                    $"{ProviderName} AI error ({httpResponse.StatusCode}): {errorBody}");

            if (attempt < maxAttempts)
            {
                TimeSpan delay = TimeSpan.FromSeconds(15);
                _logger.LogWarning("Rate limited by {Provider}. Waiting {Delay}s before retry...",
                    ProviderName, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            throw new InvalidOperationException(
                $"{ProviderName} AI rate limit reached. Please wait and try again.");
        }

        throw new InvalidOperationException($"Failed to get response from {ProviderName} AI.");
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
            _logger.LogWarning("Failed to deserialize {Provider} response as array, trying wrapped format",
                ProviderName);
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
            _logger.LogError(ex, "Failed to parse {Provider} response JSON: {Json}", ProviderName, jsonText);
        }

        return [];
    }

    private static List<ChatMessage> BuildMessagesWithUserOnly(string textContent)
    {
        string prompt = $$"""
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

            Return a JSON object with a single key "meals" containing an array where each element has exactly two fields:
            - "name": the meal/dish name (string, trimmed)
            - "price": the price as a number (decimal, e.g. 5.50)

            Example output: {"meals": [{"name": "Пилешка супа", "price": 3.50}, {"name": "Салата Цезар", "price": 4.20}]}

            Menu content:
            ---
            {{textContent}}
            ---
            """;

        return [new ChatMessage("user", prompt)];
    }

    private static List<ChatMessage> BuildMessagesWithSystem(string textContent)
    {
        string systemPrompt = """
            You are a menu parser for a food ordering system.
            Extract all meals/dishes and their prices from menu content.
            Return ONLY valid JSON with format: {"meals": [{"name": "dish name", "price": 5.50}]}
            Rules:
            - Extract prices as decimal numbers without currency symbols
            - Convert comma decimal separators to dots
            - Ignore headers, footers, dates, or non-meal content
            - Preserve original meal name language
            """;

        string userPrompt = $"Parse this menu and return JSON:\n\n{textContent}";

        return
        [
            new ChatMessage("system", systemPrompt),
            new ChatMessage("user", userPrompt)
        ];
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

        Return a JSON object with a single key "meals" containing an array where each element has exactly two fields:
        - "name": the meal/dish name (string, trimmed)
        - "price": the price as a number (decimal, e.g. 5.50)

        Example output: {"meals": [{"name": "Пилешка супа", "price": 3.50}, {"name": "Салата Цезар", "price": 4.20}]}

        Menu content:
        ---
        {{textContent}}
        ---
        """;

    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] List<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] decimal Temperature,
        [property: JsonPropertyName("response_format")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        ResponseFormat? ResponseFormat
    );

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content
    );

    private record ResponseFormat(
        [property: JsonPropertyName("type")] string Type
    );

    private record ChatResponse(
        [property: JsonPropertyName("choices")] List<ChatChoice>? Choices
    );

    private record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message
    );
}
