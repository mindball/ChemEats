using MenuParser.Abstractions;
using MenuParser.Models;
using MenuParser.TextExtraction;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MenuParser;

public class MenuFileParser : IMenuFileParser
{
    private readonly TextExtractorFactory _extractorFactory;
    private readonly IAiMealExtractor _aiExtractor;
    private readonly ILogger<MenuFileParser> _logger;

    private static readonly string[] SupportedExtensions = [".csv", ".xlsx", ".docx"];
    private static readonly Regex PriceRegex = new(@"\d+(?:[\.,]\d+)?", RegexOptions.Compiled);

    public MenuFileParser(
        TextExtractorFactory extractorFactory,
        IAiMealExtractor aiExtractor,
        ILogger<MenuFileParser> logger)
    {
        _extractorFactory = extractorFactory;
        _aiExtractor = aiExtractor;
        _logger = logger;
    }

    public bool IsSupported(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<ParsedMeal>> ParseAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(fileName);
        _logger.LogInformation("Parsing menu file: {FileName} (extension: {Extension})", fileName, extension);

        ITextExtractor? extractor = _extractorFactory.GetExtractor(extension);
        if (extractor is null)
        {
            throw new NotSupportedException(
                $"File format '{extension}' is not supported. Supported formats: {string.Join(", ", SupportedExtensions)}");
        }

        string textContent = extractor.Extract(fileStream);

        if (string.IsNullOrWhiteSpace(textContent))
        {
            _logger.LogWarning("No text content extracted from file {FileName}", fileName);
            return [];
        }

        _logger.LogInformation("Extracted {Length} characters from {FileName}", textContent.Length, fileName);

        IReadOnlyList<ParsedMeal> meals = await _aiExtractor.ExtractMealsAsync(textContent, cancellationToken);

        return meals;
    }
}
