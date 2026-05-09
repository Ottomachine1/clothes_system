namespace ClothesSystem.Web.Models.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; init; }
    public int TotalItems { get; init; }
    public int ActiveItems { get; init; }
    public int CompletedItems { get; init; }
    public IReadOnlyCollection<OwnerSummaryViewModel> Owners { get; init; } = Array.Empty<OwnerSummaryViewModel>();
}
