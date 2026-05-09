using ClothesSystem.Application.Common.Models;
using ClothesSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace ClothesSystem.Desktop.Services;

public interface IAuthenticationService
{
    CurrentUserInfo? CurrentUser { get; }
    Task<bool> LoginAsync(string username, string password);
    void Logout();
    bool IsLoggedIn { get; }
    bool IsAdmin { get; }
}

public class DesktopAuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private CurrentUserInfo? _currentUser;

    public DesktopAuthenticationService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public CurrentUserInfo? CurrentUser => _currentUser;

    public bool IsLoggedIn => _currentUser?.IsAuthenticated == true;

    public bool IsAdmin => _currentUser?.IsAdmin == true;

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                Log.Warning("Login failed: user {Username} not found", username);
                return false;
            }

            var isValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isValid)
            {
                Log.Warning("Login failed: invalid password for user {Username}", username);
                return false;
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            _currentUser = new CurrentUserInfo
            {
                UserId = user.Id,
                DisplayName = user.DisplayName ?? user.UserName ?? "用户",
                IsAuthenticated = true,
                IsAdmin = isAdmin
            };

            Log.Information("User {Username} logged in successfully, IsAdmin: {IsAdmin}", username, isAdmin);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login exception for user {Username}", username);
            throw;
        }
    }

    public void Logout()
    {
        _currentUser = null;
    }
}
