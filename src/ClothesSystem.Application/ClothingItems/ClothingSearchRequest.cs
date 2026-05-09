using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public class ClothingSearchRequest
{
    private int _page = 1;
    private int _pageSize = 12;

    public string? StyleNumber { get; set; }
    public int? Year { get; set; }
    public string? Season { get; set; }
    public string? Keyword { get; set; }
    public ClothingProgressStatus? Progress { get; set; }
    public string? OwnerId { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 12 : value > 100 ? 100 : value;
    }
}
