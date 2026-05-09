namespace ClothesSystem.Application.ClothingItems;

public class ClothingImageFileDto
{
    public string? LegacyFilePath { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public byte[]? BinaryContent { get; init; }
}
