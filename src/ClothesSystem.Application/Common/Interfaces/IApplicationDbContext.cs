using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClothesSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ClothingItem> ClothingItems { get; }

    DbSet<ModificationNote> ModificationNotes { get; }

    DbSet<FabricEntry> FabricEntries { get; }

    DbSet<ClothingImageAttachment> ClothingImageAttachments { get; }

    DbSet<ApprovalRecord> ApprovalRecords { get; }

    DbSet<StyleNumberSequence> StyleNumberSequences { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
