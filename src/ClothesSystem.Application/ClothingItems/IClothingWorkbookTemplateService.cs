namespace ClothesSystem.Application.ClothingItems;

public interface IClothingWorkbookTemplateService
{
    Task<ClothingWorkbookTemplateFileDto?> ExportPlaceholderAsync(Guid clothingItemId, CancellationToken cancellationToken = default);

    Task<ClothingWorkbookTemplateFileDto?> ExportDesignSheetAsync(Guid clothingItemId, CancellationToken cancellationToken = default);

    Task<ClothingWorkbookImportResult> ImportPlaceholderAsync(
        Guid clothingItemId,
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<ClothingDesignSheetImportResult> ImportDesignSheetAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
