using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public class ClothingItemDetailDto
{
    public Guid Id { get; init; }
    public string StyleNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Year { get; init; }
    public string Season { get; init; } = string.Empty;
    public ClothingProgressStatus Progress { get; init; }
    public string ProgressDisplayName { get; init; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; init; }
    public string ApprovalStatusDisplayName { get; init; } = string.Empty;
    public string FabricInformation { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImagePath { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerDisplayName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public IReadOnlyCollection<FabricEntryDto> FabricEntries { get; init; } = Array.Empty<FabricEntryDto>();
    public IReadOnlyCollection<ClothingImageAttachmentDto> ImageAttachments { get; init; } = Array.Empty<ClothingImageAttachmentDto>();
    public IReadOnlyCollection<ApprovalRecordDto> ApprovalRecords { get; init; } = Array.Empty<ApprovalRecordDto>();
    public IReadOnlyCollection<ModificationNoteDto> ModificationNotes { get; init; } = Array.Empty<ModificationNoteDto>();
}
