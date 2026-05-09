using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public class ApprovalRecordDto
{
    public Guid Id { get; init; }
    public ApprovalAction Action { get; init; }
    public string ActionDisplayName { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string CreatedByDisplayName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
