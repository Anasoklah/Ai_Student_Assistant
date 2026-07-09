using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class BookSectionConfiguration : IEntityTypeConfiguration<BookSection>
{
    public void Configure(EntityTypeBuilder<BookSection> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ChapterId);
        builder.HasIndex(e => new { e.ChapterId, e.SectionNumber }).IsUnique();

        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.NormalizedTitle).HasMaxLength(500);

        builder.HasOne(e => e.Chapter)
            .WithMany(e => e.Sections)
            .HasForeignKey(e => e.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
