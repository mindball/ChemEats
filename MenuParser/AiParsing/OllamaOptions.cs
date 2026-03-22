namespace MenuParser.AiParsing;

public sealed class OllamaOptions
{
    public string Host { get; set; } = "http://127.0.0.1:11434";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "qwen2.5:7b";
    public decimal Temperature { get; set; } = 0.0m;
    public decimal TopP { get; set; } = 0.9m;
    public decimal RepeatPenalty { get; set; } = 1.2m;
    public int MaxRetries { get; set; } = 1;
    public int MaxInputChars { get; set; } = 12000;
    public int HttpTimeoutSeconds { get; set; } = 240;
}
