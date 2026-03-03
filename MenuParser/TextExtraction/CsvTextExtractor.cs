using MenuParser.Abstractions;

namespace MenuParser.TextExtraction;

public class CsvTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedExtensions = [".csv", ".txt"];

    public bool CanHandle(string fileExtension) =>
        SupportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

    public string Extract(Stream fileStream)
    {
        using StreamReader reader = new(fileStream, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
