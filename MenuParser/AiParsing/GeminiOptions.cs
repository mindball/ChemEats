namespace MenuParser.AiParsing;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
    public int MaxRetries { get; set; } = 2;
}
