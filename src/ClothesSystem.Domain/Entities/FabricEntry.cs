namespace ClothesSystem.Domain.Entities;

public class FabricEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClothingItemId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ClothingItem? ClothingItem { get; set; }
}
