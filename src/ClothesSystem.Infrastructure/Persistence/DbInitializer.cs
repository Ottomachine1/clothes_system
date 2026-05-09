using ClothesSystem.Domain.Entities;
using ClothesSystem.Domain.Enums;
using ClothesSystem.Infrastructure.Identity;
using ClothesSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesSystem.Infrastructure.Persistence;

public static class DbInitializer
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";
    private const string AdminEmail = "admin@clothes.local";
    private const string AdminUserName = AdminEmail;
    private const string DemoAdminPassword = "Admin123!";
    private const string DemoDesignerPassword = "Designer123!";

    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        string contentRootPath,
        bool seedDemoUsers,
        bool seedDemoClothing,
        bool resetDemoPasswords,
        bool seedDefaultAdmin,
        bool resetDefaultAdminPassword)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var context = scopedProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();
        await BackfillLegacyDataAsync(context, contentRootPath);

        var imageMigrationService = scopedProvider.GetRequiredService<IImageAttachmentMigrationService>();
        await imageMigrationService.MoveDatabaseImagesToFileStorageAsync();

        await EnsureRoleAsync(roleManager, AdminRole);
        await EnsureRoleAsync(roleManager, UserRole);

        ApplicationUser? adminUser = null;
        ApplicationUser? designerUser = null;

        if (seedDefaultAdmin)
        {
            adminUser = await EnsureUserAsync(
                userManager,
                AdminUserName,
                AdminEmail,
                DemoAdminPassword,
                "System Admin",
                resetPassword: resetDefaultAdminPassword);

            await EnsureUserRoleAsync(userManager, adminUser, AdminRole);
            await EnsureUserRoleAsync(userManager, adminUser, UserRole);
        }

        if (seedDemoUsers)
        {
            adminUser = await EnsureUserAsync(
                userManager,
                AdminUserName,
                AdminEmail,
                DemoAdminPassword,
                "System Admin",
                resetPassword: resetDemoPasswords);
            designerUser = await EnsureUserAsync(userManager, "designer@clothes.local", DemoDesignerPassword, "Designer");

            await EnsureUserRoleAsync(userManager, adminUser, AdminRole);
            await EnsureUserRoleAsync(userManager, designerUser, UserRole);
        }

        if (seedDemoClothing)
        {
            adminUser ??= await userManager.FindByEmailAsync(AdminEmail);
            await SeedDemoClothingAsync(context, adminUser, designerUser);
        }
    }

    private static async Task SeedDemoClothingAsync(
        ApplicationDbContext context,
        ApplicationUser? adminUser,
        ApplicationUser? designerUser)
    {
        if (adminUser == null)
        {
            return;
        }

        var existingStyleNumbers = await context.ClothingItems
            .AsNoTracking()
            .Where(item => item.StyleNumber == "DEMO-2026-001" || item.StyleNumber == "DEMO-2026-002")
            .Select(item => item.StyleNumber)
            .ToListAsync();

        if (existingStyleNumbers.Count == 2)
        {
            return;
        }

        var designerOwner = designerUser ?? adminUser;
        var now = DateTime.UtcNow;
        var items = new List<ClothingItem>();

        var jacket = new ClothingItem
        {
            StyleNumber = "DEMO-2026-001",
            Title = "Demo Commuter Light Jacket",
            Year = 2026,
            Season = "Spring",
            Progress = ClothingProgressStatus.SampleMaking,
            ApprovalStatus = ApprovalStatus.Pending,
            FabricInformation = "Nylon blend / 135g / water-resistant; mesh lining / breathable / panel insert",
            Description = "Demo daily commute outerwear. Shoulder line and cuff ease can be used for sample review.",
            OwnerId = designerOwner.Id,
            OwnerDisplayName = GetDisplayName(designerOwner),
            CreatedAtUtc = now.AddDays(-12),
            UpdatedAtUtc = now.AddDays(-2)
        };

        jacket.FabricEntries.Add(new FabricEntry
        {
            MaterialName = "Nylon blend",
            Specification = "135g / water-resistant",
            Remark = "Main fabric",
            SortOrder = 0
        });
        jacket.FabricEntries.Add(new FabricEntry
        {
            MaterialName = "Mesh lining",
            Specification = "Breathable",
            Remark = "Panel insert",
            SortOrder = 1
        });
        jacket.ApprovalRecords.Add(new ApprovalRecord
        {
            Action = ApprovalAction.Submitted,
            Comment = "Sample is ready for approval.",
            CreatedByUserId = designerOwner.Id,
            CreatedByDisplayName = GetDisplayName(designerOwner),
            CreatedAtUtc = now.AddDays(-1)
        });

        var dress = new ClothingItem
        {
            StyleNumber = "DEMO-2026-002",
            Title = "Demo Printed Summer Dress",
            Year = 2026,
            Season = "Summer",
            Progress = ClothingProgressStatus.Revision,
            ApprovalStatus = ApprovalStatus.ChangesRequested,
            FabricInformation = "Rayon poplin / digital print / shrinkage check pending; lightweight lining / soft / color card pending",
            Description = "Demo dress for showing modification notes and approval flow. Waistline and hem curve still need confirmation.",
            OwnerId = adminUser.Id,
            OwnerDisplayName = GetDisplayName(adminUser),
            CreatedAtUtc = now.AddDays(-20),
            UpdatedAtUtc = now.AddDays(-1)
        };

        dress.FabricEntries.Add(new FabricEntry
        {
            MaterialName = "Rayon poplin",
            Specification = "Digital print / shrinkage check pending",
            Remark = "Shell fabric",
            SortOrder = 0
        });
        dress.FabricEntries.Add(new FabricEntry
        {
            MaterialName = "Lightweight lining",
            Specification = "Soft",
            Remark = "Color card pending",
            SortOrder = 1
        });
        dress.ApprovalRecords.Add(new ApprovalRecord
        {
            Action = ApprovalAction.Submitted,
            Comment = "Submitted for approval.",
            CreatedByUserId = adminUser.Id,
            CreatedByDisplayName = GetDisplayName(adminUser),
            CreatedAtUtc = now.AddDays(-4)
        });
        dress.ApprovalRecords.Add(new ApprovalRecord
        {
            Action = ApprovalAction.ReturnedForChanges,
            Comment = "Please adjust the waistline and review the print color card first.",
            CreatedByUserId = adminUser.Id,
            CreatedByDisplayName = GetDisplayName(adminUser),
            CreatedAtUtc = now.AddDays(-3)
        });

        if (!existingStyleNumbers.Contains(jacket.StyleNumber))
        {
            items.Add(jacket);
        }

        if (!existingStyleNumbers.Contains(dress.StyleNumber))
        {
            items.Add(dress);
        }

        context.ClothingItems.AddRange(items);

        var notes = new List<ModificationNote>();
        if (items.Contains(jacket))
        {
            notes.Add(new ModificationNote
            {
                ClothingItemId = jacket.Id,
                Content = "Sleeve opening is slightly tight. Recommend adding 1.5 cm ease.",
                CreatedByUserId = designerOwner.Id,
                CreatedByDisplayName = GetDisplayName(designerOwner),
                CreatedAtUtc = now.AddDays(-5)
            });
        }

        if (items.Contains(dress))
        {
            notes.Add(new ModificationNote
            {
                ClothingItemId = dress.Id,
                Content = "Print saturation is a bit high. Prepare a second color card for confirmation.",
                CreatedByUserId = adminUser.Id,
                CreatedByDisplayName = GetDisplayName(adminUser),
                CreatedAtUtc = now.AddDays(-3)
            });
        }

        context.ModificationNotes.AddRange(notes);

        await context.SaveChangesAsync();
    }

    private static string GetDisplayName(ApplicationUser user) =>
        user.DisplayName ?? user.Email ?? user.UserName ?? user.Id;

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName)
    {
        return await EnsureUserAsync(userManager, email, email, password, displayName, resetPassword: false);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string email,
        string password,
        string displayName,
        bool resetPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        user ??= await userManager.FindByNameAsync(userName);

        if (user != null)
        {
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
                await userManager.UpdateAsync(user);
            }

            if (resetPassword)
            {
                await ResetPasswordAsync(userManager, user, password);
            }

            return user;
        }

        user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errorMessage = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create seed user {email}: {errorMessage}");
        }

        return user;
    }

    private static async Task ResetPasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        var hasPassword = await userManager.HasPasswordAsync(user);
        IdentityResult result;

        if (hasPassword)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            result = await userManager.ResetPasswordAsync(user, token, password);
        }
        else
        {
            result = await userManager.AddPasswordAsync(user, password);
        }

        if (!result.Succeeded)
        {
            var errorMessage = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to reset seed user password for {user.UserName}: {errorMessage}");
        }
    }

    private static async Task EnsureUserRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string roleName)
    {
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static async Task BackfillLegacyDataAsync(ApplicationDbContext context, string contentRootPath)
    {
        var fabricEntriesToAdd = await context.ClothingItems
            .AsNoTracking()
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.FabricInformation) &&
                !context.FabricEntries.Any(entry => entry.ClothingItemId == item.Id))
            .Select(item => new
            {
                item.Id,
                item.FabricInformation
            })
            .ToListAsync();

        if (fabricEntriesToAdd.Count > 0)
        {
            context.FabricEntries.AddRange(
                fabricEntriesToAdd.Select(item => new FabricEntry
                {
                    ClothingItemId = item.Id,
                    MaterialName = item.FabricInformation!,
                    Specification = string.Empty,
                    Remark = "Migrated from legacy fabric text",
                    SortOrder = 0
                }));
        }

        var existingAttachmentItemIds = await context.ClothingImageAttachments
            .AsNoTracking()
            .Select(attachment => attachment.ClothingItemId)
            .Distinct()
            .ToListAsync();

        var legacyImageItems = await context.ClothingItems
            .AsNoTracking()
            .Where(item => !string.IsNullOrWhiteSpace(item.ImagePath))
            .Select(item => new
            {
                item.Id,
                item.ImagePath,
                item.OwnerId,
                item.OwnerDisplayName,
                item.UpdatedAtUtc
            })
            .ToListAsync();

        foreach (var item in legacyImageItems.Where(item => !existingAttachmentItemIds.Contains(item.Id)))
        {
            var legacyImage = ReadLegacyImage(contentRootPath, item.ImagePath!);
            if (legacyImage == null)
            {
                continue;
            }

            context.ClothingImageAttachments.Add(new ClothingImageAttachment
            {
                ClothingItemId = item.Id,
                FilePath = item.ImagePath,
                OriginalFileName = Path.GetFileName(item.ImagePath!),
                ContentType = legacyImage.Value.ContentType,
                BinaryContent = legacyImage.Value.BinaryContent,
                UploadedByUserId = item.OwnerId,
                UploadedByDisplayName = item.OwnerDisplayName,
                UploadedAtUtc = item.UpdatedAtUtc,
                SortOrder = 0
            });
        }

        var legacyAttachments = await context.ClothingImageAttachments
            .Where(attachment =>
                !string.IsNullOrWhiteSpace(attachment.FilePath) &&
                (attachment.BinaryContent == null ||
                 attachment.BinaryContent.Length == 0 ||
                 string.IsNullOrWhiteSpace(attachment.ContentType)))
            .ToListAsync();

        foreach (var attachment in legacyAttachments)
        {
            var legacyImage = ReadLegacyImage(contentRootPath, attachment.FilePath!);
            if (legacyImage == null)
            {
                continue;
            }

            attachment.ContentType = legacyImage.Value.ContentType;
            attachment.BinaryContent = legacyImage.Value.BinaryContent;
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    private static (string ContentType, byte[] BinaryContent)? ReadLegacyImage(string contentRootPath, string relativePath)
    {
        var normalized = relativePath.TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var webRootPath = Path.Combine(contentRootPath, "wwwroot");
        var physicalPath = Path.Combine(webRootPath, normalized);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        var extension = Path.GetExtension(physicalPath);
        var contentType = extension.ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return (contentType, File.ReadAllBytes(physicalPath));
    }
}
