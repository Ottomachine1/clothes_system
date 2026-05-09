namespace ClothesSystem.Web.Models.Admin;

public class OwnerSummaryViewModel
{
    public string OwnerId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public int ActiveItemCount { get; init; }
}
