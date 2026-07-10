using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.ChapterId);
        builder.HasIndex(e => e.SectionId);
        builder.HasIndex(e => e.PageNumber);

        // pgvector index for fast similarity search
        builder.HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.Property(e => e.ChapterTitle).HasMaxLength(300);
        builder.Property(e => e.SectionTitle).HasMaxLength(300);
        builder.Property(e => e.Content).HasMaxLength(10000);
        builder.Property(e => e.Embedding).HasColumnType("vector(768)"); // nomic-embed-text dimension

        builder.HasOne(e => e.Document)
            .WithMany(e => e.Chunks)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Chapter)
            .WithMany()
            .HasForeignKey(e => e.ChapterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Section)
            .WithMany()
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
