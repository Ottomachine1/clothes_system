using System.Security.Claims;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Models;
using ClothesSystem.Web.Authorization;

namespace ClothesSystem.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserInfo GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return CurrentUserInfo.Anonymous;
        }

        return new CurrentUserInfo
        {
            UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            DisplayName =
                principal.FindFirstValue(ClaimTypes.Email) ??
                principal.FindFirstValue(ClaimTypes.Name) ??
                principal.Identity.Name ??
                "已登录用户",
            IsAuthenticated = true,
            IsAdmin = principal.IsInRole(ApplicationRoles.Admin)
        };
    }
}
