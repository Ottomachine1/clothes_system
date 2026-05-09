namespace ClothesSystem.Domain.Entities;

public class StyleNumberSequence
{
    public string Period { get; set; } = string.Empty;
    public int LastSequence { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
