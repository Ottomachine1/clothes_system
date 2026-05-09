namespace ClothesSystem.Web.Models.ClothingItems;

public class ExistingImageAttachmentViewModel
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string UploadedByDisplayName { get; init; } = string.Empty;
    public DateTime UploadedAtUtc { get; init; }
}
