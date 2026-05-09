using ClothesSystem.Application.Common.Interfaces;

namespace ClothesSystem.Infrastructure.Services;

public class FileSystemClothingImageStorageService : IClothingImageStorageService
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly IDataPathProvider _dataPathProvider;

    public FileSystemClothingImageStorageService(IDataPathProvider dataPathProvider)
    {
        _dataPathProvider = dataPathProvider;
    }

    public async Task<string> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content.Length == 0)
        {
            throw new InvalidOperationException("Image content is empty.");
        }

        var now = DateTime.UtcNow;
        var extension = ResolveExtension(originalFileName, contentType);
        var relativePath = Path.Combine(
            now.ToString("yyyy"),
            now.ToString("MM"),
            $"{Guid.NewGuid():N}{extension}");
        var fullPath = ResolveStoragePath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var fullPath = ResolveStoragePath(relativePath);
        return File.Exists(fullPath)
            ? await File.ReadAllBytesAsync(fullPath, cancellationToken)
            : null;
    }

    private string ResolveStoragePath(string relativePath)
    {
        var rootPath = Path.GetFullPath(_dataPathProvider.ImageStoragePath);
        var normalizedRelativePath = relativePath
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelativePath));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Image path is outside the configured storage directory.");
        }

        return fullPath;
    }

    private static string ResolveExtension(string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg" or ".png" or ".webp")
        {
            return extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : extension.ToLowerInvariant();
        }

        return ExtensionByContentType.TryGetValue(contentType, out var contentTypeExtension)
            ? contentTypeExtension
            : ".bin";
    }
}
