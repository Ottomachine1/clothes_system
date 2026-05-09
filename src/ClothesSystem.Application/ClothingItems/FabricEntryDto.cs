namespace ClothesSystem.Application.ClothingItems;

public class FabricEntryDto
{
    public Guid Id { get; init; }
    public string MaterialName { get; init; } = string.Empty;
    public string Specification { get; init; } = string.Empty;
    public string Remark { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
