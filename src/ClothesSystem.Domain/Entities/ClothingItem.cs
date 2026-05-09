using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Domain.Entities;

public class ClothingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StyleNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Season { get; set; } = string.Empty;
    public ClothingProgressStatus Progress { get; set; } = ClothingProgressStatus.PatternMaking;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public string FabricInformation { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ModificationNote> ModificationNotes { get; set; } = new List<ModificationNote>();
    public ICollection<FabricEntry> FabricEntries { get; set; } = new List<FabricEntry>();
    public ICollection<ClothingImageAttachment> ImageAttachments { get; set; } = new List<ClothingImageAttachment>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
}
