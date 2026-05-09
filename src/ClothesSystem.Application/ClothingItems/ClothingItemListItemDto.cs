using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public class ClothingItemListItemDto
{
    public Guid Id { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string StyleNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Year { get; init; }
    public string Season { get; init; } = string.Empty;
    public ClothingProgressStatus Progress { get; init; }
    public string ProgressDisplayName { get; init; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; init; }
    public string ApprovalStatusDisplayName { get; init; } = string.Empty;
    public string FabricInformation { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public string OwnerDisplayName { get; init; } = string.Empty;
    public DateTime UpdatedAtUtc { get; init; }
}
