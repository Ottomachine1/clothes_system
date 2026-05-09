namespace ClothesSystem.Infrastructure.Services.Workbook;

internal sealed class SingleStyleWorkbookTemplateDefinition
{
    public static SingleStyleWorkbookTemplateDefinition Default { get; } = new();

    public string RelativeTemplatePath { get; } = Path.Combine("Templates", "SingleStyleDesignTemplate.xlsx");
    public string WorksheetName { get; } = "Sheet1";
    public string StyleNumberCellReference { get; } = "B2";
    public string QuarterCellReference { get; } = "D2";
    public string DesignerCellReference { get; } = "F2";
    public string OrderDateCellReference { get; } = "B3";
    public string PatternMakerCellReference { get; } = "D3";
    public string SampleMakerCellReference { get; } = "F3";
    public string ImagePlaceholderCellReference { get; } = "A4";
    public string ModificationNotesCellReference { get; } = "G4";
    public string SummaryCellReference { get; } = "G20";
    public string ProductNameCellReference { get; } = "B25";
    public string ApprovalCellReference { get; } = "A38";
    public WorkbookImagePlacement ImagePlacement { get; } = new(1, 4, 6, 24, 8);
    public int ProductNameMaxLength { get; } = 18;
    public int ModificationNotesMaxCharacters { get; } = 420;
    public int SummaryMaxCharacters { get; } = 280;
    public int FabricBlockMaxCharacters { get; } = 160;
    public int ApprovalMaxCharacters { get; } = 60;
    public IReadOnlyList<WorkbookFabricBlockDefinition> FabricBlocks { get; } = new[]
    {
        new WorkbookFabricBlockDefinition("A32", "面料A"),
        new WorkbookFabricBlockDefinition("D32", "面料B"),
        new WorkbookFabricBlockDefinition("F32", "面料C"),
        new WorkbookFabricBlockDefinition("H32", "里料")
    };
}

internal readonly record struct WorkbookImagePlacement(
    int StartColumnIndex,
    int StartRowIndex,
    int EndColumnIndex,
    int EndRowIndex,
    int PaddingPixels);

internal readonly record struct WorkbookFabricBlockDefinition(
    string CellReference,
    string Label);
