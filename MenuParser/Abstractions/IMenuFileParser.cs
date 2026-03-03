using MenuParser.Models;

namespace MenuParser.Abstractions;

public interface IMenuFileParser
{
    Task<IReadOnlyList<ParsedMeal>> ParseAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    bool IsSupported(string fileName);
}
