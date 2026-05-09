using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Models;
using ClothesSystem.Application.Common.Services;
using ClothesSystem.Domain.Entities;
using ClothesSystem.Domain.Enums;
using ClothesSystem.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Tests;

public class ClothingServiceTests : IDisposable
{
    private readonly string _rootPath;
    private readonly TestImageStorageService _imageStorageService = new();
    private CurrentUserInfo _currentUser = new()
    {
        UserId = "user-1",
        DisplayName = "User 1",
        IsAuthenticated = true
    };

    public ClothingServiceTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "ClothesSystem.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task SearchAsync_NonAdminOnlySeesOwnedItems()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ClothingItems.AddRange(
            CreateItem("OWN-001", "user-1", "User 1"),
            CreateItem("OTHER-001", "user-2", "User 2"));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SearchAsync(new ClothingSearchRequest());

        Assert.Single(result.Items);
        Assert.Equal("OWN-001", result.Items.Single().StyleNumber);
    }

    [Fact]
    public async Task SearchAsync_AdminSeesAllItems()
    {
        _currentUser = new CurrentUserInfo
        {
            UserId = "admin",
            DisplayName = "Admin",
            IsAuthenticated = true,
            IsAdmin = true
        };
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ClothingItems.AddRange(
            CreateItem("OWN-001", "user-1", "User 1"),
            CreateItem("OTHER-001", "user-2", "User 2"));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SearchAsync(new ClothingSearchRequest());

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task SubmitAndReviewApprovalAsync_UpdatesStatusAndProgress()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var item = CreateItem("APP-001", "user-1", "User 1");
        dbContext.ClothingItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var submitted = await service.SubmitForApprovalAsync(item.Id, "ready");

        Assert.True(submitted);
        Assert.Equal(ApprovalStatus.Pending, (await dbContext.ClothingItems.FindAsync(item.Id))!.ApprovalStatus);

        _currentUser = new CurrentUserInfo
        {
            UserId = "admin",
            DisplayName = "Admin",
            IsAuthenticated = true,
            IsAdmin = true
        };

        var reviewed = await service.ReviewApprovalAsync(item.Id, ApprovalAction.Approved, "ok");

        var updatedItem = await dbContext.ClothingItems
            .Include(clothingItem => clothingItem.ApprovalRecords)
            .SingleAsync(clothingItem => clothingItem.Id == item.Id);
        Assert.True(reviewed);
        Assert.Equal(ApprovalStatus.Approved, updatedItem.ApprovalStatus);
        Assert.Equal(ClothingProgressStatus.Confirmed, updatedItem.Progress);
        Assert.Equal(2, updatedItem.ApprovalRecords.Count);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ClothingService CreateService(ApplicationDbContext dbContext) =>
        new(
            dbContext,
            new TestCurrentUserService(() => _currentUser),
            new TestUserDirectoryService(),
            new TestStyleNumberGenerator(),
            _imageStorageService);

    private ApplicationDbContext CreateDbContext()
    {
        var databasePath = Path.Combine(_rootPath, $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ClothingItem CreateItem(string styleNumber, string ownerId, string ownerDisplayName) =>
        new()
        {
            StyleNumber = styleNumber,
            Title = styleNumber,
            Year = 2026,
            Season = "Spring",
            FabricInformation = "Cotton",
            OwnerId = ownerId,
            OwnerDisplayName = ownerDisplayName
        };

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly Func<CurrentUserInfo> _currentUserAccessor;

        public TestCurrentUserService(Func<CurrentUserInfo> currentUserAccessor)
        {
            _currentUserAccessor = currentUserAccessor;
        }

        public CurrentUserInfo GetCurrentUser() => _currentUserAccessor();
    }

    private sealed class TestUserDirectoryService : IUserDirectoryService
    {
        public Task<IReadOnlyCollection<OwnerOption>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<OwnerOption>>(Array.Empty<OwnerOption>());

        public Task<OwnerOption?> FindOwnerAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<OwnerOption?>(new OwnerOption
            {
                UserId = userId,
                DisplayName = userId,
                Email = $"{userId}@test.local"
            });
    }

    private sealed class TestStyleNumberGenerator : IStyleNumberGenerator
    {
        public Task<string> GenerateNextStyleNumberAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("DYYK-260510000");
    }

    private sealed class TestImageStorageService : IClothingImageStorageService
    {
        public Task<string> SaveAsync(
            byte[] content,
            string originalFileName,
            string contentType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"test/{originalFileName}");

        public Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(null);
    }
}
