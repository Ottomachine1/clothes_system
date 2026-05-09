using ClothesSystem.Domain.Entities;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Tests;

public class StyleNumberGeneratorTests : IDisposable
{
    private readonly string _rootPath;

    public StyleNumberGeneratorTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "ClothesSystem.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task GenerateNextStyleNumberAsync_StartsMonthlySequenceAtConfiguredStart()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var generator = new StyleNumberGenerator(dbContext);

        var styleNumber = await generator.GenerateNextStyleNumberAsync();

        Assert.Equal($"DYYK-{DateTime.Now:yyMM}10000", styleNumber);
    }

    [Fact]
    public async Task GenerateNextStyleNumberAsync_IncrementsPersistedMonthlySequence()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var generator = new StyleNumberGenerator(dbContext);

        var firstStyleNumber = await generator.GenerateNextStyleNumberAsync();
        var secondStyleNumber = await generator.GenerateNextStyleNumberAsync();

        Assert.Equal($"DYYK-{DateTime.Now:yyMM}10000", firstStyleNumber);
        Assert.Equal($"DYYK-{DateTime.Now:yyMM}10001", secondStyleNumber);
    }

    [Fact]
    public async Task GenerateNextStyleNumberAsync_SeedsFromExistingLegacyStyleNumbers()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ClothingItems.Add(new ClothingItem
        {
            StyleNumber = $"DYYK-{DateTime.Now:yyMM}10042",
            Title = "Legacy item",
            Year = DateTime.Now.Year,
            Season = "Spring",
            FabricInformation = "Cotton",
            OwnerId = "user-1",
            OwnerDisplayName = "User 1"
        });
        await dbContext.SaveChangesAsync();
        var generator = new StyleNumberGenerator(dbContext);

        var styleNumber = await generator.GenerateNextStyleNumberAsync();

        Assert.Equal($"DYYK-{DateTime.Now:yyMM}10043", styleNumber);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_rootPath, "test.db")}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
