namespace ClothesSystem.Application.Common.Interfaces;

public interface IClothingImageStorageService
{
    Task<string> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);
}
