using ClothesSystem.Application.ClothingItems;

namespace ClothesSystem.Web.Models;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; init; }
    public bool IsAdmin { get; init; }
    public bool ShowDemoCredentials { get; init; }
    public ClothingDashboardSummaryDto? Summary { get; init; }
}
