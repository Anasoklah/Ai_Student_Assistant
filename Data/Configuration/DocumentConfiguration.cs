using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => e.GradeLevel);
        builder.HasIndex(e => e.DocumentType);
        builder.HasIndex(e => e.UploadedByUserId);
        builder.HasIndex(e => e.IsApproved);
        builder.HasIndex(e => new { e.Subject, e.GradeLevel, e.DocumentType, e.IsApproved });

        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Subject)
            .HasConversion<string>()
            .HasMaxLength(100);
        builder.Property(e => e.GradeLevel)
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(e => e.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DocumentType.OfficialBook);
        builder.Property(e => e.SourceName).HasMaxLength(300);
        builder.Property(e => e.Edition).HasMaxLength(100);
        builder.Property(e => e.Language).HasMaxLength(20);
        builder.Property(e => e.FilePath).HasMaxLength(1000);

        builder.HasMany(e => e.Chunks)
            .WithOne(e => e.Document)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedByUser)
            .WithMany(e => e.UploadedDocuments)
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
