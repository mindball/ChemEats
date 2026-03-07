namespace MenuParser.AiParsing;

public sealed class GroqOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public int MaxRetries { get; set; } = 2;
}
