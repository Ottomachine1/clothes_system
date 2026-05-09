using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClothesSystem.Web.Models.ClothingItems;

public class ClothingItemSearchViewModel
{
    public ClothingSearchRequest Search { get; init; } = new();
    public PaginatedResult<ClothingItemListItemDto> Result { get; init; } = new();
    public IReadOnlyCollection<SelectListItem> ProgressOptions { get; init; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> OwnerOptions { get; init; } = Array.Empty<SelectListItem>();
    public bool IsAdmin { get; init; }
}
