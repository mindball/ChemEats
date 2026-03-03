using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MenuParser.Abstractions;

namespace MenuParser.TextExtraction;

public class ExcelTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedExtensions = [".xlsx"];

    public bool CanHandle(string fileExtension) =>
        SupportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

    public string Extract(Stream fileStream)
    {
        StringBuilder builder = new();

        using SpreadsheetDocument document = SpreadsheetDocument.Open(fileStream, false);
        WorkbookPart? workbookPart = document.WorkbookPart;

        if (workbookPart is null)
            return string.Empty;

        SharedStringTablePart? sharedStringTable = workbookPart.SharedStringTablePart;

        foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
        {
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData is null)
                continue;

            foreach (Row row in sheetData.Elements<Row>())
            {
                List<string> cellValues = [];

                foreach (Cell cell in row.Elements<Cell>())
                {
                    string cellValue = GetCellValue(cell, sharedStringTable);
                    cellValues.Add(cellValue);
                }

                if (cellValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                    builder.AppendLine(string.Join(";", cellValues));
            }
        }

        return builder.ToString();
    }

    private static string GetCellValue(Cell cell, SharedStringTablePart? sharedStringTable)
    {
        if (cell.CellValue is null)
            return string.Empty;

        string value = cell.CellValue.Text;

        if (cell.DataType?.Value == CellValues.SharedString && sharedStringTable is not null)
        {
            if (int.TryParse(value, out int index))
                return sharedStringTable.SharedStringTable.ElementAt(index).InnerText;
        }

        return value;
    }
}
