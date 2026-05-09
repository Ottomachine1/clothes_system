using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class FabricEntryConfiguration : IEntityTypeConfiguration<FabricEntry>
{
    public void Configure(EntityTypeBuilder<FabricEntry> builder)
    {
        builder.ToTable("FabricEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.MaterialName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entry => entry.Specification)
            .HasMaxLength(200);

        builder.Property(entry => entry.Remark)
            .HasMaxLength(300);

        builder.Property(entry => entry.SortOrder)
            .IsRequired();
    }
}
