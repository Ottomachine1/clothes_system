namespace ClothesSystem.Application.ClothingItems;

public class ModificationNoteDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string CreatedByDisplayName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
