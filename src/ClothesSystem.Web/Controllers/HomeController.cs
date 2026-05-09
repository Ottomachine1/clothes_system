using System.Diagnostics;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Web.Authorization;
using ClothesSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothesSystem.Web.Controllers;

public class HomeController : Controller
{
    private readonly IClothingService _clothingService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HomeController(
        IClothingService clothingService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _clothingService = clothingService;
        _configuration = configuration;
        _environment = environment;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new HomeIndexViewModel
            {
                ShowDemoCredentials = _environment.IsDevelopment() || _configuration.GetValue("SeedDemoUsers", false)
            });
        }

        var summary = await _clothingService.GetDashboardSummaryAsync(cancellationToken);
        return View(new HomeIndexViewModel
        {
            IsAuthenticated = true,
            IsAdmin = User.IsInRole(ApplicationRoles.Admin),
            ShowDemoCredentials = _environment.IsDevelopment() || _configuration.GetValue("SeedDemoUsers", false),
            Summary = summary
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
