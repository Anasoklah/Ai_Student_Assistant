using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.LastActiveAt);
        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => new { e.UserId, e.LastActiveAt });

        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Subject)
            .HasConversion<string>()
            .HasMaxLength(100);
        builder.Property(e => e.Mode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ChatMode.Explain);

        builder.HasMany(e => e.Messages)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(e => e.ChatSessions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
