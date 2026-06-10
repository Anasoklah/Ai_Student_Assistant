using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain;

namespace SyrianStudyBot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Title).IsRequired();
            e.Property(d => d.Subject).IsRequired();
            e.Property(d => d.SourceName).IsRequired();
            e.Property(d => d.Edition).HasMaxLength(50);
            e.Property(d => d.Language).HasMaxLength(20);
            e.HasIndex(d => d.Subject);
            e.HasIndex(d => d.SourceName);
            e.HasMany(d => d.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(e =>
        {
            e.HasKey(c => c.Id);
            // nomic-embed-text produces 768-dimensional vectors
            e.Property(c => c.Embedding).HasColumnType("vector(768)");
            e.Property(c => c.Content).IsRequired();
            e.Property(c => c.ChapterTitle).HasMaxLength(200);
            e.Property(c => c.SectionTitle).HasMaxLength(200);
            e.HasIndex(c => c.DocumentId);
            e.HasIndex(c => c.PageNumber);
        });

        modelBuilder.Entity<UserSession>(e =>
        {
            e.HasKey(s => s.TelegramUserId);
            // TelegramUserId comes from Telegram — never auto-generate it
            e.Property(s => s.TelegramUserId).ValueGeneratedNever();
        });

        modelBuilder.Entity<QuizSession>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Questions).HasColumnType("jsonb");
            e.Property(q => q.Answers).HasColumnType("jsonb");
            e.HasIndex(q => q.TelegramUserId);
        });
    }
}
