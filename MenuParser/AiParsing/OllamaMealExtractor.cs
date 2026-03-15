using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuParser.AiParsing;

public class OllamaMealExtractor : IAiMealExtractor
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaMealExtractor> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OllamaMealExtractor(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaMealExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // ВАЖНО: Увеличаваме Timeout-а, за да дадем време на локалния модел да генерира отговора
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<IReadOnlyList<ParsedMeal>> ExtractMealsAsync(
        string textContent,
        CancellationToken cancellationToken = default)
    {
        string inputText = LimitInput(textContent, _options.MaxInputChars);
        string prompt = BuildPrompt(inputText);

        // Конфигурираме заявката към Ollama
        GenerateRequest request = new(
            _options.Model,
            prompt,
            false,   // Без стрийминг за по-лесна десериализация
            "json",  // Инструктираме Ollama да форматира като JSON
            false,
            new GenerateOptions(0.1m) // Ниска температура за по-голяма точност
        );

        try
        {
            HttpResponseMessage response = await SendWithRetryAsync(request, cancellationToken);

            GenerateResponse? generateResponse = await response.Content
                .ReadFromJsonAsync<GenerateResponse>(JsonOptions, cancellationToken);

            string? jsonText = generateResponse?.Response;
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Ollama returned empty response.");
                return [];
            }

            List<ParsedMeal> parsedMeals = DeserializeMeals(jsonText);

            // Филтрираме само валидните резултати
            List<ParsedMeal> validMeals = parsedMeals
                .Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Price > 0)
                .ToList();

            _logger.LogInformation("Successfully parsed {Count} meals from menu via Ollama ({Model})",
                validMeals.Count, _options.Model);

            return validMeals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Ollama meal extraction");
            return [];
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        GenerateRequest request,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Clamp(_options.MaxRetries, 1, 3);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            string endpoint = BuildGenerateEndpoint(_options.Host);
            _logger.LogInformation("Ollama API request attempt {Attempt}/{Max} to {Endpoint}",
                attempt, maxAttempts, endpoint);

            using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint);
            httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

            try
            {
                HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                if (httpResponse.IsSuccessStatusCode)
                    return httpResponse;

                string errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Ollama API returned {StatusCode}: {Error}",
                    httpResponse.StatusCode, errorBody);

                if (!ShouldRetry(httpResponse.StatusCode) || attempt == maxAttempts)
                {
                    throw new InvalidOperationException($"Ollama AI error ({httpResponse.StatusCode}): {errorBody}");
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Ollama request timed out after {Timeout}s. Check if GPU is overloaded.", _httpClient.Timeout.TotalSeconds);
                throw new TimeoutException("Ollama took too long to respond.", ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException("Failed to get response from Ollama AI.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
        or HttpStatusCode.RequestTimeout
        or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.GatewayTimeout;

    private static string BuildGenerateEndpoint(string host)
    {
        string normalizedHost = string.IsNullOrWhiteSpace(host)
            ? "http://127.0.0.1:11434"
            : host.TrimEnd('/');

        return $"{normalizedHost}/api/generate";
    }

    private static string LimitInput(string textContent, int maxInputChars)
    {
        int maxChars = Math.Max(1000, maxInputChars);
        return textContent.Length <= maxChars ? textContent : textContent[..maxChars];
    }

    private List<ParsedMeal> DeserializeMeals(string jsonText)
    {
        try
        {
            // 1. Опит за десериализация на обвиващия обект {"meals": [...]}
            var wrapped = JsonSerializer.Deserialize<OllamaJsonWrapper>(jsonText, JsonOptions);
            if (wrapped?.Meals != null) return wrapped.Meals;
        }
        catch (JsonException) { /* Продължаваме към fallback */ }

        try
        {
            // 2. Fallback: Търсене на масив вътре в JSON обекта ръчно
            using JsonDocument document = JsonDocument.Parse(jsonText);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var meals = property.Value.Deserialize<List<ParsedMeal>>(JsonOptions);
                        if (meals != null) return meals;
                    }
                }
            }
            // 3. Последен опит: директно като масив
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return document.RootElement.Deserialize<List<ParsedMeal>>(JsonOptions) ?? [];
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Ollama response JSON: {Json}", jsonText);
        }

        return [];
    }

    private static string BuildPrompt(string textContent)
    {
        return $$"""
                 System: You are a specialized data extractor. Input is a Bulgarian menu. Output MUST be a valid JSON object.
                 
                 User: Extract all food items and their prices from the text below.
                 
                 Constraints:
                 1. Output ONLY a JSON object with a "meals" key. No conversation, no markdown code blocks, no preamble.
                 2. Names: Keep original Bulgarian names exactly as they appear.
                 3. Prices: Extract as decimal numbers. Remove currency symbols like "lv.", "лв.", "€".
                 4. If a line is not a meal or doesn't have a price, skip it.
                 5. Ensure the JSON is minified and valid.

                 Example format:
                 {"meals":[{"name":"Шопска салата","price":8.50},{"name":"Мусака","price":12.00}]}

                 Menu text to process:
                 {{textContent}}
                 """;
    }

    // Помощни рекорди за JSON структурата
    private sealed record OllamaJsonWrapper(
        [property: JsonPropertyName("meals")] List<ParsedMeal> Meals
    );

    private sealed record GenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("raw")] bool Raw,
        [property: JsonPropertyName("options")] GenerateOptions Options
    );

    private sealed record GenerateOptions(
        [property: JsonPropertyName("temperature")] decimal Temperature
    );

    private sealed record GenerateResponse(
        [property: JsonPropertyName("response")] string? Response
    );
}