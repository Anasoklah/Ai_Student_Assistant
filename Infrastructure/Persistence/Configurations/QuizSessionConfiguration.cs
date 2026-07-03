using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class QuizSessionConfiguration : IEntityTypeConfiguration<QuizSession>
{
    public void Configure(EntityTypeBuilder<QuizSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => e.IsCompleted);

        builder.Property(e => e.Subject)
            .HasConversion<string>()
            .HasMaxLength(100);
        builder.Property(e => e.GradeLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithMany(e => e.QuizSessions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
