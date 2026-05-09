namespace ClothesSystem.Application.ClothingItems;

public class ClothingImageAttachmentInputDto
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] BinaryContent { get; set; } = Array.Empty<byte>();
}
