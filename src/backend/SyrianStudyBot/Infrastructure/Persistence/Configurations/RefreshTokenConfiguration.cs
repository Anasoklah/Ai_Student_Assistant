using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
            builder.HasKey(rt => rt.Id);

            builder.HasIndex(rt => rt.Token).IsUnique();
            builder.HasIndex(rt => rt.UserId);
            builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt, rt.IsRevoked, rt.IsReplaced });

            builder.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            builder.Property(rt => rt.UserId).IsRequired();
            builder.Property(rt => rt.CreatedByIp).HasMaxLength(100);
            builder.Property(rt => rt.RevocationReason).HasMaxLength(500);

            // Relationship
            builder.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
    }
}
