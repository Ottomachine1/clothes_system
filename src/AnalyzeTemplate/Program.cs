using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AnalyzeTemplate;

class Program
{
    static void Main(string[] args)
    {
        var templatePath = args.Length > 0 ? args[0] : "D:/clothes_system/template/设计单空白.xlsx";
        Console.WriteLine($"分析模板: {templatePath}\n");

        using var spreadsheet = SpreadsheetDocument.Open(templatePath, false);
        var workbookPart = spreadsheet.WorkbookPart!;

        // Get shared strings
        var sharedStrings = workbookPart.GetPartsOfType<SharedStringTablePart>()
            .FirstOrDefault();
        var strings = new List<string>();
        if (sharedStrings != null)
        {
            foreach (var item in sharedStrings.SharedStringTable.Elements<SharedStringItem>())
            {
                strings.Add(item.Text?.Text ?? item.InnerText ?? "");
            }
        }

        Console.WriteLine("=== 共享字符串表 ===");
        for (int i = 0; i < strings.Count; i++)
        {
            Console.WriteLine($"  [{i}]: {strings[i]}");
        }

        // Get worksheet
        var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault();
        Console.WriteLine($"\n=== 工作表: {sheet?.Name} ===");

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet!.Id!.Value!);
        var worksheet = worksheetPart.Worksheet;

        // Get all cells with values
        Console.WriteLine("\n=== 单元格内容 ===");
        var cells = worksheet.Descendants<Cell>()
            .Where(c => c.CellValue != null)
            .OrderBy(c => GetRowIndex(c.CellReference?.Value))
            .ThenBy(c => GetColumnIndex(c.CellReference?.Value));

        foreach (var cell in cells)
        {
            var refStr = cell.CellReference?.Value ?? "?";
            var value = cell.CellValue?.Text ?? "";
            var dataType = cell.DataType?.Value;

            string displayValue = value;
            if (dataType == CellValues.SharedString && int.TryParse(value, out int idx) && idx < strings.Count)
            {
                displayValue = $"\"{strings[idx]}\"";
            }

            Console.WriteLine($"  {refStr}: {displayValue}");
        }
    }

    static uint GetRowIndex(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        var digits = new string(cellRef.Where(char.IsDigit).ToArray());
        return uint.TryParse(digits, out var row) ? row : 0;
    }

    static int GetColumnIndex(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        var letters = new string(cellRef.Where(char.IsLetter).ToArray()).ToUpperInvariant();
        var colIndex = 0;
        foreach (var letter in letters)
        {
            colIndex = (colIndex * 26) + (letter - 'A' + 1);
        }
        return colIndex;
    }
}