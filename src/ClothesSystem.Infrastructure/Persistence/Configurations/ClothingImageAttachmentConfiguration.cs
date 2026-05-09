using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class ClothingImageAttachmentConfiguration : IEntityTypeConfiguration<ClothingImageAttachment>
{
    public void Configure(EntityTypeBuilder<ClothingImageAttachment> builder)
    {
        builder.ToTable("ClothingImageAttachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.FilePath)
            .HasMaxLength(300);

        builder.Property(attachment => attachment.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(100);

        builder.Property(attachment => attachment.BinaryContent);

        builder.Property(attachment => attachment.UploadedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(attachment => attachment.UploadedByDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(attachment => attachment.UploadedAtUtc)
            .IsRequired();

        builder.Property(attachment => attachment.SortOrder)
            .IsRequired();
    }
}
