using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums; // Adjusted to match your class namespace

namespace SyrianStudyBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ===================== KEYS & INDEXES =====================
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.SubscriptionTier);
        builder.HasIndex(e => e.LastMessageReset);


        builder.HasIndex(e => e.GradeLevel);

        // ===================== PROPERTIES =====================
        // Basic Info
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FullName)
            .HasMaxLength(200);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Profile
        builder.Property(e => e.GradeLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("ar");

        // Subscription
        builder.Property(e => e.SubscriptionTier)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(SubscriptionTier.Free);

        // Usage Tracking
        builder.Property(e => e.MessagesToday)
            .HasDefaultValue(0);

        builder.Property(e => e.LastMessageReset)
            .IsRequired();

        builder.Property(e => e.UploadsThisMonth)
            .HasDefaultValue(0);

        builder.Property(e => e.LastUploadReset)
            .IsRequired();

        // User relationships are configured from the dependent entity configurations.
    }
}
