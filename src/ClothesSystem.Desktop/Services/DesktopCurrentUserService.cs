using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Models;

namespace ClothesSystem.Desktop.Services;

public class DesktopCurrentUserService : ICurrentUserService
{
    private readonly IAuthenticationService _authService;

    public DesktopCurrentUserService(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public CurrentUserInfo GetCurrentUser()
    {
        return _authService.CurrentUser ?? CurrentUserInfo.Anonymous;
    }
}
