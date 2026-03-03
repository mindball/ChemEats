using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MenuParser.Abstractions;

namespace MenuParser.TextExtraction;

public class WordTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedExtensions = [".docx"];

    public bool CanHandle(string fileExtension) =>
        SupportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

    public string Extract(Stream fileStream)
    {
        StringBuilder builder = new();

        using WordprocessingDocument document = WordprocessingDocument.Open(fileStream, false);
        Body? body = document.MainDocumentPart?.Document.Body;

        if (body is null)
            return string.Empty;

        foreach (Paragraph paragraph in body.Elements<Paragraph>())
        {
            string text = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
                builder.AppendLine(text);
        }

        foreach (Table table in body.Elements<Table>())
        {
            foreach (TableRow row in table.Elements<TableRow>())
            {
                List<string> cellTexts = [];

                foreach (TableCell cell in row.Elements<TableCell>())
                {
                    cellTexts.Add(cell.InnerText.Trim());
                }

                if (cellTexts.Any(t => !string.IsNullOrWhiteSpace(t)))
                    builder.AppendLine(string.Join(";", cellTexts));
            }
        }

        return builder.ToString();
    }
}
