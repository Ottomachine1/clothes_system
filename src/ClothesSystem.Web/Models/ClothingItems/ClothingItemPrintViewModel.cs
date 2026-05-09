using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Web.Models.ClothingItems;

public class ClothingItemPrintViewModel
{
    public ClothingItemDetailDto Item { get; init; } = new();
    public string? PrimaryImageBase64 { get; init; }
    public string PrimaryImageContentType { get; init; } = "image/png";
    public string? SecondaryImageBase64 { get; init; }
    public string SecondaryImageContentType { get; init; } = "image/png";

    public string QuarterDisplay => BuildQuarterDisplay(Item.Year, Item.Season);
    public string OrderDateDisplay => Item.CreatedAtUtc.ToLocalTime().ToString("yyyy/MM/dd");

    public IReadOnlyList<string> PrimaryImageNotes =>
        BuildNotes(Item.Description, Item.ModificationNotes);

    public IReadOnlyList<string> SecondaryImageNotes =>
        BuildNotes(Item.Description, Item.ModificationNotes);

    private static string BuildQuarterDisplay(int year, string? season)
    {
        if (string.IsNullOrWhiteSpace(season))
            return year.ToString();

        var trimmed = season.Trim();
        var normalized = trimmed.ToLowerInvariant();

        var code = normalized.Contains("春") || normalized.Contains("夏") ||
                   normalized.Contains("spring") || normalized.Contains("summer") || normalized == "ss"
            ? "SS"
            : (normalized.Contains("秋") || normalized.Contains("冬") ||
               normalized.Contains("autumn") || normalized.Contains("fall") ||
               normalized.Contains("winter") || normalized is "aw" or "fw")
                ? "AW"
                : null;

        return code == null ? trimmed : $"{year}{code}";
    }

    private static IReadOnlyList<string> BuildNotes(
        string? description,
        IReadOnlyCollection<ModificationNoteDto> notes)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(description))
            lines.Add($"款式说明：{description.Trim()}");

        foreach (var note in notes.OrderByDescending(n => n.CreatedAtUtc).Take(5))
        {
            lines.Add($"{note.CreatedAtUtc.ToLocalTime():MM/dd} {note.CreatedByDisplayName}：{note.Content.Trim()}");
        }

        return lines.Count == 0 ? new[] { "暂无修改意见" } : lines;
    }
}
