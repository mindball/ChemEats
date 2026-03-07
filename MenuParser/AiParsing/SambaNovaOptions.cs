namespace MenuParser.AiParsing;

public sealed class SambaNovaOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "Meta-Llama-3.3-70B-Instruct";
    public int MaxRetries { get; set; } = 2;
}
