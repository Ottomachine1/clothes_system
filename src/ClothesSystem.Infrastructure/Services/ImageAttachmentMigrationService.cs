using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Infrastructure.Services;

public interface IImageAttachmentMigrationService
{
    Task<int> MoveDatabaseImagesToFileStorageAsync(CancellationToken cancellationToken = default);
}

public class ImageAttachmentMigrationService : IImageAttachmentMigrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IClothingImageStorageService _imageStorageService;

    public ImageAttachmentMigrationService(
        ApplicationDbContext dbContext,
        IClothingImageStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _imageStorageService = imageStorageService;
    }

    public async Task<int> MoveDatabaseImagesToFileStorageAsync(CancellationToken cancellationToken = default)
    {
        var attachments = await _dbContext.ClothingImageAttachments
            .Where(attachment => attachment.BinaryContent != null && attachment.BinaryContent.Length > 0)
            .OrderBy(attachment => attachment.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        var migratedCount = 0;
        foreach (var attachment in attachments)
        {
            if (attachment.BinaryContent is not { Length: > 0 })
            {
                continue;
            }

            var filePath = await _imageStorageService.SaveAsync(
                attachment.BinaryContent,
                attachment.OriginalFileName,
                attachment.ContentType ?? "application/octet-stream",
                cancellationToken);

            attachment.FilePath = filePath;
            attachment.BinaryContent = null;
            migratedCount++;
        }

        if (migratedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return migratedCount;
    }
}
