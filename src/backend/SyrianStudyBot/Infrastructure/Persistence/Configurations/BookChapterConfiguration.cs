using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class BookChapterConfiguration : IEntityTypeConfiguration<BookChapter>
{
    public void Configure(EntityTypeBuilder<BookChapter> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => new { e.DocumentId, e.ChapterNumber }).IsUnique();

        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.NormalizedTitle).HasMaxLength(500);

        builder.HasOne(e => e.Document)
            .WithMany(e => e.Chapters)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Sections)
            .WithOne(e => e.Chapter)
            .HasForeignKey(e => e.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
