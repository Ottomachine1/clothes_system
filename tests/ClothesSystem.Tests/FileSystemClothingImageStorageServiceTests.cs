using ClothesSystem.Infrastructure.Services;

namespace ClothesSystem.Tests;

public class FileSystemClothingImageStorageServiceTests : IDisposable
{
    private readonly string _rootPath;
    private readonly FileSystemClothingImageStorageService _storageService;

    public FileSystemClothingImageStorageServiceTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "ClothesSystem.Tests", Guid.NewGuid().ToString("N"));
        _storageService = new FileSystemClothingImageStorageService(new TestDataPathProvider(_rootPath));
    }

    [Fact]
    public async Task SaveAsync_WritesImageAndReturnsRelativePath()
    {
        var content = new byte[] { 1, 2, 3, 4 };

        var relativePath = await _storageService.SaveAsync(content, "sample.png", "image/png");

        Assert.DoesNotContain("..", relativePath);
        Assert.EndsWith(".png", relativePath);
        Assert.True(File.Exists(Path.Combine(_rootPath, "images", relativePath.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task ReadAsync_ReturnsSavedContent()
    {
        var content = new byte[] { 9, 8, 7 };
        var relativePath = await _storageService.SaveAsync(content, "sample.jpg", "image/jpeg");

        var loadedContent = await _storageService.ReadAsync(relativePath);

        Assert.Equal(content, loadedContent);
    }

    [Fact]
    public async Task ReadAsync_RejectsPathOutsideImageStorage()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _storageService.ReadAsync("../outside.jpg"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
