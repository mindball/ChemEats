namespace MenuParser.AiParsing;

[Obsolete("SambaNova requires a paid plan and is no longer part of the fallback chain. Use GeminiOptions instead.")]
public sealed class SambaNovaOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "Meta-Llama-3.3-70B-Instruct";
    public int MaxRetries { get; set; } = 2;
}
