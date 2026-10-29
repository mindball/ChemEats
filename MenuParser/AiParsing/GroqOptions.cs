namespace MenuParser.AiParsing;

public sealed class GroqOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-120b";
    public int MaxRetries { get; set; } = 2;
}
