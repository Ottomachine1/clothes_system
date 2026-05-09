using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.ToTable("ApprovalRecords");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Comment)
            .HasMaxLength(2000);

        builder.Property(record => record.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(record => record.CreatedByDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(record => record.CreatedAtUtc)
            .IsRequired();
    }
}
