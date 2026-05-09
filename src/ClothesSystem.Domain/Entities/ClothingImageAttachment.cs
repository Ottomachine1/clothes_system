namespace ClothesSystem.Domain.Entities;

public class ClothingImageAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClothingItemId { get; set; }
    public string? FilePath { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public byte[]? BinaryContent { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public string UploadedByDisplayName { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public int SortOrder { get; set; }
    public ClothingItem? ClothingItem { get; set; }
}
