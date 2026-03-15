namespace MenuParser.AiParsing;

public sealed class OllamaOptions
{
    public string Host { get; set; } = "http://127.0.0.1:11434";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "Qwen2.5:3b";
    public int MaxRetries { get; set; } = 1;
    public int MaxInputChars { get; set; } = 4000;
    public int HttpTimeoutSeconds { get; set; } = 240;
}
