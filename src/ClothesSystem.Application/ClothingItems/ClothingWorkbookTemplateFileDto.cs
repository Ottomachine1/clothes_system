namespace ClothesSystem.Application.ClothingItems;

public class ClothingWorkbookTemplateFileDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/csv";
    public byte[] Content { get; init; } = Array.Empty<byte>();
}
