namespace MenuParser.Abstractions;

public interface ITextExtractor
{
    string Extract(Stream fileStream);
    bool CanHandle(string fileExtension);
}
