namespace ClothesSystem.Infrastructure.Services.Workbook;

internal sealed class BlankDesignTemplateDefinition
{
    public static BlankDesignTemplateDefinition Default { get; } = new();

    // Template file location
    public string RelativeTemplatePath { get; } = "设计单空白.xlsx";
    public string WorksheetName { get; } = "Sheet1";

    // Header section (Row 2)
    public string StyleNumberCellReference { get; } = "B2";
    public string QuarterCellReference { get; } = "D2";
    public string DesignerCellReference { get; } = "F2";

    // Order date, pattern maker, sample maker (Row 3)
    public string OrderDateCellReference { get; } = "B3";
    public string PatternMakerCellReference { get; } = "D3";
    public string SampleMakerCellReference { get; } = "F3";

    // Image placeholder (Row 4)
    public string ImagePlaceholderCellReference { get; } = "A4";

    // Modification notes area (G4:J18)
    public string ModificationNotesCellReference { get; } = "G4";

    // Second image placeholder (G4:J18)
    public string SecondImagePlaceholderCellReference { get; } = "G4";

    // Image placement area for second image (G4:J18)
    public WorkbookImagePlacement SecondImagePlacement { get; } = new(7, 4, 10, 18, 5);

    // Second image notes table start row (G19:J24 - below second image)
    public string SecondImageNotesTableCellReference { get; } = "G19";
    public int SecondImageNotesTableMaxRows { get; } = 6;

    // Product name (Row 25)
    public string ProductNameCellReference { get; } = "A25";

    // Size table labels (Rows 26-31)
    public string SizeTableStartRow { get; } = "26";
    public string SizeTableEndRow { get; } = "31";

    // Fabric blocks (Rows 32-37)
    public string FabricACellReference { get; } = "A32";
    public string FabricBCellReference { get; } = "D32";
    public string FabricCCellReference { get; } = "F32";
    public string LiningCellReference { get; } = "H32";
    public IReadOnlyList<WorkbookFabricBlockDefinition> FabricBlocks { get; } = new[]
    {
        new WorkbookFabricBlockDefinition("A32", "面料A"),
        new WorkbookFabricBlockDefinition("D32", "面料B"),
        new WorkbookFabricBlockDefinition("F32", "面料C"),
        new WorkbookFabricBlockDefinition("H32", "里料")
    };

    // Approval section (Row 38)
    public string ApprovalCellReference { get; } = "A38";

    // Image placement area
    public WorkbookImagePlacement ImagePlacement { get; } = new(1, 4, 6, 24, 5);

    // Max character limits
    public int ProductNameMaxLength { get; } = 20;
    public int ModificationNotesMaxCharacters { get; } = 500;
    public int SizeValueMaxLength { get; } = 10;
    public int FabricBlockMaxCharacters { get; } = 200;
    public int ApprovalMaxCharacters { get; } = 100;
}
