using ClothesSystem.Application.Common.Models;
using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public interface IClothingService
{
    Task<ClothingDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateNextStyleNumberAsync(CancellationToken cancellationToken = default);

    Task<PaginatedResult<ClothingItemListItemDto>> SearchAsync(ClothingSearchRequest request, CancellationToken cancellationToken = default);

    Task<ClothingImageFileDto?> GetImageFileAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    Task<ClothingItemDetailDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClothingItemEditDto?> GetEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(ClothingItemEditDto input, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, ClothingItemEditDto input, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AddNoteAsync(Guid id, string content, CancellationToken cancellationToken = default);

    Task<bool> SubmitForApprovalAsync(Guid id, string? comment, CancellationToken cancellationToken = default);

    Task<bool> ReviewApprovalAsync(Guid id, ApprovalAction action, string? comment, CancellationToken cancellationToken = default);
}
