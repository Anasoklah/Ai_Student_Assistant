using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.DocumentId);
        
        // pgvector index for fast similarity search
        builder.HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.Property(e => e.ChapterTitle).HasMaxLength(300);
        builder.Property(e => e.SectionTitle).HasMaxLength(300);
        builder.Property(e => e.Content).HasMaxLength(10000);
        builder.Property(e => e.Embedding).HasColumnType("vector(768)"); // nomic-embed-text dimension
    }
}