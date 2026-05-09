namespace ClothesSystem.Application.ClothingItems;

public class ClothingImageAttachmentDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string UploadedByDisplayName { get; init; } = string.Empty;
    public DateTime UploadedAtUtc { get; init; }
    public int SortOrder { get; init; }
    public byte[]? BinaryContent { get; init; }
}
