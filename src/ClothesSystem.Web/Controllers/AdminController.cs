using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Domain.Enums;
using ClothesSystem.Web.Authorization;
using ClothesSystem.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClothesSystem.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
[EnableRateLimiting("api")]
public class AdminController : Controller
{
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IClothingService _clothingService;

    public AdminController(IUserDirectoryService userDirectoryService, IClothingService clothingService)
    {
        _userDirectoryService = userDirectoryService;
        _clothingService = clothingService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var owners = await _userDirectoryService.GetOwnerOptionsAsync(cancellationToken);
        var searchRequest = new ClothingSearchRequest { PageSize = int.MaxValue };
        var result = await _clothingService.SearchAsync(searchRequest, cancellationToken);

        var ownerSummaries = owners
            .Select(owner => new OwnerSummaryViewModel
            {
                OwnerId = owner.UserId,
                DisplayName = owner.DisplayName,
                Email = owner.Email,
                ItemCount = result.Items.Count(item => item.OwnerId == owner.UserId),
                ActiveItemCount = result.Items.Count(item =>
                    item.OwnerId == owner.UserId &&
                    item.Progress != ClothingProgressStatus.Pass)
            })
            .OrderByDescending(owner => owner.ItemCount)
            .ThenBy(owner => owner.DisplayName)
            .ToList();

        return View(new AdminDashboardViewModel
        {
            TotalUsers = owners.Count,
            TotalItems = result.TotalCount,
            ActiveItems = result.Items.Count(item => item.Progress != ClothingProgressStatus.Pass),
            CompletedItems = result.Items.Count(item => item.Progress == ClothingProgressStatus.Pass),
            Owners = ownerSummaries
        });
    }
}
