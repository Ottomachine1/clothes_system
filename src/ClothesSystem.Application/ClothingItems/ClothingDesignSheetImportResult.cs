namespace ClothesSystem.Application.ClothingItems;

public class ClothingDesignSheetImportResult
{
    public bool Success { get; set; }
    public Guid? NewItemId { get; set; }
    public string? StyleNumber { get; set; }
    public string? Title { get; set; }
    public string Message { get; set; } = "";
    public List<string> Warnings { get; set; } = new();
}

public class ClothingDesignSheetData
{
    public string StyleNumber { get; set; } = "";
    public int Year { get; set; }
    public string? Season { get; set; }
    public string? Designer { get; set; }
    public string? ProductName { get; set; }
    public DateTime? OrderDate { get; set; }
    public List<FabricEntryEditDto> FabricEntries { get; set; } = new();
}