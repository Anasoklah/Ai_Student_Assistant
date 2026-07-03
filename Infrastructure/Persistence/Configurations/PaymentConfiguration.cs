using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ProviderTransactionId);
        builder.HasIndex(e => new { e.UserId, e.Status, e.CreatedAt });

        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.ProviderTransactionId).HasMaxLength(500);
        builder.Property(e => e.ProviderResponse).HasMaxLength(2000);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.TargetTier)
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(e => e.Method)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PaymentMethod.ShamCash);
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PaymentStatus.Pending);

        builder.HasOne(e => e.User)
            .WithMany(e => e.Payments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
