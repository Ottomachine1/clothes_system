namespace ClothesSystem.Domain.Entities;

public class ModificationNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClothingItemId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ClothingItem? ClothingItem { get; set; }
}
