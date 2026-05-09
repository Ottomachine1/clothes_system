using ClothesSystem.Domain.Entities;
using ClothesSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class ClothingItemConfiguration : IEntityTypeConfiguration<ClothingItem>
{
    public void Configure(EntityTypeBuilder<ClothingItem> builder)
    {
        builder.ToTable("ClothingItems");

        builder.HasKey(item => item.Id);

        builder.HasIndex(item => item.StyleNumber)
            .IsUnique();

        builder.Property(item => item.StyleNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(item => item.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(item => item.Year)
            .IsRequired();

        builder.Property(item => item.Season)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(item => item.ApprovalStatus)
            .IsRequired()
            .HasDefaultValue(ApprovalStatus.Draft)
            .HasSentinel(ApprovalStatus.Draft);

        builder.Property(item => item.FabricInformation)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(item => item.Description)
            .HasMaxLength(2000);

        builder.Property(item => item.ImagePath)
            .HasMaxLength(300);

        builder.Property(item => item.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(item => item.OwnerDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.CreatedAtUtc)
            .IsRequired();

        builder.Property(item => item.UpdatedAtUtc)
            .IsRequired();

        builder.HasMany(item => item.ModificationNotes)
            .WithOne(note => note.ClothingItem)
            .HasForeignKey(note => note.ClothingItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.FabricEntries)
            .WithOne(entry => entry.ClothingItem)
            .HasForeignKey(entry => entry.ClothingItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.ImageAttachments)
            .WithOne(attachment => attachment.ClothingItem)
            .HasForeignKey(attachment => attachment.ClothingItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.ApprovalRecords)
            .WithOne(record => record.ClothingItem)
            .HasForeignKey(record => record.ClothingItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
