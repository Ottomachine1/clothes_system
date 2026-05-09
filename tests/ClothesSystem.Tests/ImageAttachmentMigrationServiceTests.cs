using ClothesSystem.Domain.Entities;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Tests;

public class ImageAttachmentMigrationServiceTests : IDisposable
{
    private readonly string _rootPath;
    private readonly TestDataPathProvider _pathProvider;

    public ImageAttachmentMigrationServiceTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "ClothesSystem.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
        _pathProvider = new TestDataPathProvider(_rootPath);
    }

    [Fact]
    public async Task MoveDatabaseImagesToFileStorageAsync_MovesBinaryContentToFile()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var item = new ClothingItem
        {
            StyleNumber = "TEST-001",
            Title = "Test item",
            Year = 2026,
            Season = "Spring",
            FabricInformation = "Cotton",
            OwnerId = "user-1",
            OwnerDisplayName = "User 1"
        };
        var attachment = new ClothingImageAttachment
        {
            ClothingItemId = item.Id,
            OriginalFileName = "sample.png",
            ContentType = "image/png",
            BinaryContent = new byte[] { 1, 3, 5, 7 },
            UploadedByUserId = "user-1",
            UploadedByDisplayName = "User 1"
        };

        dbContext.ClothingItems.Add(item);
        dbContext.ClothingImageAttachments.Add(attachment);
        await dbContext.SaveChangesAsync();

        var storageService = new FileSystemClothingImageStorageService(_pathProvider);
        var migrationService = new ImageAttachmentMigrationService(dbContext, storageService);

        var migratedCount = await migrationService.MoveDatabaseImagesToFileStorageAsync();

        var migratedAttachment = await dbContext.ClothingImageAttachments.SingleAsync();
        Assert.Equal(1, migratedCount);
        Assert.Null(migratedAttachment.BinaryContent);
        Assert.False(string.IsNullOrWhiteSpace(migratedAttachment.FilePath));
        Assert.Equal(new byte[] { 1, 3, 5, 7 }, await storageService.ReadAsync(migratedAttachment.FilePath!));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_rootPath, "test.db")}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
