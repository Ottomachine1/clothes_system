using System.IO;
using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Desktop.Services;

public interface IDesktopImageService
{
    Task<ClothingImageAttachmentInputDto?> LoadFromFileAsync(string filePath, CancellationToken ct = default);
    string[] SupportedExtensions { get; }
}

public class DesktopImageService : IDesktopImageService
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public string[] SupportedExtensions => ContentTypes.Keys.ToArray();

    public async Task<ClothingImageAttachmentInputDto?> LoadFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("图片大小不能超过 5 MB。");
        }

        var extension = fileInfo.Extension;
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("图片仅支持 JPG、PNG 或 WEBP 格式。");
        }

        var bytes = await File.ReadAllBytesAsync(filePath, ct);

        return new ClothingImageAttachmentInputDto
        {
            OriginalFileName = fileInfo.Name,
            ContentType = contentType,
            BinaryContent = bytes
        };
    }
}
