using System.IO;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Infrastructure.Identity;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Desktop.Services;

public interface IDatabaseMigrationService
{
    Task EnsureDatabaseReadyAsync(CancellationToken ct = default);
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";
    private const string AdminUserName = "admin";
    private const string AdminEmail = "admin@clothes.local";
    private const string AdminPassword = "Admin123!";
    private const string DesignerUserName = "designer";
    private const string DesignerEmail = "designer@clothes.local";
    private const string DesignerPassword = "Designer123!";

    private readonly IDataPathProvider _pathProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IImageAttachmentMigrationService _imageAttachmentMigrationService;

    public DatabaseMigrationService(
        IDataPathProvider pathProvider,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IImageAttachmentMigrationService imageAttachmentMigrationService)
    {
        _pathProvider = pathProvider;
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _imageAttachmentMigrationService = imageAttachmentMigrationService;
    }

    public async Task EnsureDatabaseReadyAsync(CancellationToken ct = default)
    {
        if (NeedsMigrationFromWeb())
        {
            MigrateFromWeb();
        }

        await _dbContext.Database.MigrateAsync(ct);
        await _imageAttachmentMigrationService.MoveDatabaseImagesToFileStorageAsync(ct);
        await SeedDefaultDataAsync(ct);
    }

    private bool NeedsMigrationFromWeb()
    {
        if (File.Exists(_pathProvider.DatabasePath))
        {
            return false;
        }

        return _pathProvider is DesktopDataPathProvider desktopProvider &&
            !string.IsNullOrEmpty(desktopProvider.GetLegacyWebDbPath());
    }

    private void MigrateFromWeb()
    {
        if (_pathProvider is not DesktopDataPathProvider desktopProvider)
        {
            return;
        }

        var legacyPath = desktopProvider.GetLegacyWebDbPath();
        if (string.IsNullOrEmpty(legacyPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(_pathProvider.DatabasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(legacyPath, _pathProvider.DatabasePath, overwrite: false);
    }

    private async Task SeedDefaultDataAsync(CancellationToken ct)
    {
        await EnsureRoleAsync(AdminRole);
        await EnsureRoleAsync(UserRole);

        var adminUser = await EnsureUserAsync(
            AdminUserName,
            AdminEmail,
            AdminPassword,
            "Administrator",
            ct);
        await EnsureUserRoleAsync(adminUser, AdminRole);
        await EnsureUserRoleAsync(adminUser, UserRole);

        var designerUser = await EnsureUserAsync(
            DesignerUserName,
            DesignerEmail,
            DesignerPassword,
            "Designer",
            ct);
        await EnsureUserRoleAsync(designerUser, UserRole);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string userName,
        string email,
        string password,
        string displayName,
        CancellationToken ct)
    {
        var user = await _userManager.FindByNameAsync(userName)
            ?? await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(BuildIdentityErrorMessage("Failed to create seed user", createResult));
            }

            return user;
        }

        var shouldUpdate = false;
        if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            user.UserName = userName;
            shouldUpdate = true;
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.EmailConfirmed = true;
            shouldUpdate = true;
        }

        if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            user.DisplayName = displayName;
            shouldUpdate = true;
        }

        if (shouldUpdate)
        {
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(BuildIdentityErrorMessage("Failed to update seed user", updateResult));
            }
        }

        ct.ThrowIfCancellationRequested();
        return user;
    }

    private async Task EnsureUserRoleAsync(ApplicationUser user, string roleName)
    {
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            await _userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static string BuildIdentityErrorMessage(string prefix, IdentityResult result)
    {
        var errors = string.Join(", ", result.Errors.Select(error => error.Description));
        return $"{prefix}: {errors}";
    }
}
