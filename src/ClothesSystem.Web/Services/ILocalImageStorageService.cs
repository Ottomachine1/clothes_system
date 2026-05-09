using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Web.Services;

public interface ILocalImageStorageService
{
    Task<ClothingImageAttachmentInputDto?> SaveNewAsync(IFormFile? file, CancellationToken cancellationToken = default);
}
