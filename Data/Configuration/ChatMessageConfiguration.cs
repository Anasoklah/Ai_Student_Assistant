using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain.Entities;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.Role);

        builder.Property(e => e.Content).HasMaxLength(50000);
        builder.Property(e => e.SourcesJson).HasMaxLength(2000);
    }
}