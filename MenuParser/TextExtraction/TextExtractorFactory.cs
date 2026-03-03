using MenuParser.Abstractions;

namespace MenuParser.TextExtraction;

public class TextExtractorFactory
{
    private readonly IReadOnlyList<ITextExtractor> _extractors;

    public TextExtractorFactory(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors.ToList();
    }

    public ITextExtractor? GetExtractor(string fileExtension) =>
        _extractors.FirstOrDefault(e => e.CanHandle(fileExtension));

    public bool IsSupported(string fileExtension) =>
        _extractors.Any(e => e.CanHandle(fileExtension));
}
