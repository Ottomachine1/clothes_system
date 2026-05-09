using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Domain.Entities;

public class ApprovalRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClothingItemId { get; set; }
    public ApprovalAction Action { get; set; }
    public string? Comment { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ClothingItem? ClothingItem { get; set; }
}
