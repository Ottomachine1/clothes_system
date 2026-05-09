using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class StyleNumberSequenceConfiguration : IEntityTypeConfiguration<StyleNumberSequence>
{
    public void Configure(EntityTypeBuilder<StyleNumberSequence> builder)
    {
        builder.ToTable("StyleNumberSequences");

        builder.HasKey(sequence => sequence.Period);

        builder.Property(sequence => sequence.Period)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sequence => sequence.LastSequence)
            .IsRequired();

        builder.Property(sequence => sequence.UpdatedAtUtc)
            .IsRequired();
    }
}
