using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;

public class QuizResultConfiguration : IEntityTypeConfiguration<QuizResult>
{
    public void Configure(EntityTypeBuilder<QuizResult> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => e.CompletedAt);

        builder.Property(e => e.Subject).HasMaxLength(100);
    }
}