using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Domain.Entities;
using ClothesSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClothesSystem.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>,
        IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClothingItem> ClothingItems => Set<ClothingItem>();

    public DbSet<ModificationNote> ModificationNotes => Set<ModificationNote>();

    public DbSet<FabricEntry> FabricEntries => Set<FabricEntry>();

    public DbSet<ClothingImageAttachment> ClothingImageAttachments => Set<ClothingImageAttachment>();

    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

    public DbSet<StyleNumberSequence> StyleNumberSequences => Set<StyleNumberSequence>();

    DatabaseFacade IApplicationDbContext.Database => Database;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
