using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesSystem.Infrastructure.Persistence.Configurations;

public class ModificationNoteConfiguration : IEntityTypeConfiguration<ModificationNote>
{
    public void Configure(EntityTypeBuilder<ModificationNote> builder)
    {
        builder.ToTable("ModificationNotes");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(note => note.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(note => note.CreatedByDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(note => note.CreatedAtUtc)
            .IsRequired();
    }
}
