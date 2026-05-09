using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Services;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        return AddInfrastructureCore(services, connectionString);
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        return AddInfrastructureCore(services, connectionString);
    }

    private static IServiceCollection AddInfrastructureCore(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IClothingService, ClothingService>();
        services.AddScoped<IClothingWorkbookTemplateService, ClothingWorkbookTemplateService>();
        services.AddScoped<IUserDirectoryService, UserDirectoryService>();
        services.AddScoped<IStyleNumberGenerator, StyleNumberGenerator>();
        services.AddScoped<IClothingImageStorageService, FileSystemClothingImageStorageService>();
        services.AddScoped<IImageAttachmentMigrationService, ImageAttachmentMigrationService>();

        return services;
    }
}
