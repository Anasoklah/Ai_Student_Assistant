using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

public class AppDbContext : IdentityDbContext <ApplicationUser , IdentityRole<Guid> , Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Existing entities (updated)
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();    
    // Chat System
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // Quiz System
    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();

    // Payments & Usage
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DailyUsageLog> DailyUsageLogs => Set<DailyUsageLog>();

    //Refresh Token 
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Automatically applies all configurations from the executing assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}