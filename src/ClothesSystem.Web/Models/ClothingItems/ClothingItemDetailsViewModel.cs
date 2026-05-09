using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Web.Models.ClothingItems;

public class ClothingItemDetailsViewModel
{
    public ClothingItemDetailDto Item { get; init; } = new();
    public bool IsAdmin { get; init; }
    public bool CanSubmitApproval { get; init; }
    public bool CanReviewApproval { get; init; }
}
