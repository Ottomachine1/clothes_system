using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Web.Services;

public class LocalImageStorageService : ILocalImageStorageService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    public async Task<ClothingImageAttachmentInputDto?> SaveNewAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("图片仅支持 JPG、PNG 或 WEBP 格式。");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("图片大小不能超过 5 MB。");
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return new ClothingImageAttachmentInputDto
        {
            OriginalFileName = string.IsNullOrWhiteSpace(file.FileName) ? $"image{extension}" : file.FileName,
            ContentType = contentType,
            BinaryContent = stream.ToArray()
        };
    }
}
