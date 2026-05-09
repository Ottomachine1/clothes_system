using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Models;
using ClothesSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Infrastructure.Services;

public class UserDirectoryService : IUserDirectoryService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserDirectoryService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<OwnerOption>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default) =>
        await _userManager.Users
            .OrderBy(user => user.Email)
            .Select(user => new OwnerOption
            {
                UserId = user.Id,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? user.UserName ?? user.Id) : user.DisplayName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

    public async Task<OwnerOption?> FindOwnerAsync(string userId, CancellationToken cancellationToken = default) =>
        await _userManager.Users
            .Where(user => user.Id == userId)
            .Select(user => new OwnerOption
            {
                UserId = user.Id,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? user.UserName ?? user.Id) : user.DisplayName,
                Email = user.Email ?? string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);
}
