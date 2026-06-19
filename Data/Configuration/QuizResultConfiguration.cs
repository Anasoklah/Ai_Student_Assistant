using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;

public class QuizResultConfiguration : IEntityTypeConfiguration<QuizResult>
{
    public void Configure(EntityTypeBuilder<QuizResult> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.QuizSessionId)
            .IsUnique();
        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => e.CompletedAt);

        builder.Property(e => e.Subject).HasMaxLength(100);

        builder.HasOne(e => e.User)
            .WithMany(e => e.QuizResults)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuizSession)
            .WithOne(e => e.Result)
            .HasForeignKey<QuizResult>(e => e.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
