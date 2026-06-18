using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums; // Adjusted to match your class namespace

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ===================== KEYS & INDEXES =====================
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.SubscriptionTier);
        builder.HasIndex(e => e.LastMessageReset);
        
        // Added standard lookup indexes for performance (optional but recommended)
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.GradeLevel);

        // ===================== PROPERTIES =====================
        // Basic Info
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FullName)
            .HasMaxLength(200);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(50); // Typical standard for phone numbers including country codes

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Profile
        builder.Property(e => e.GradeLevel)
            .HasConversion<string>() // Stores enum as string, or remove this line to store as int
            .HasMaxLength(50);

        builder.Property(e => e.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("ar");

        // Subscription
        builder.Property(e => e.SubscriptionTier)
            .HasConversion<string>() // Stores enum as string, or remove this line to store as int
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

        // ===================== RELATIONSHIPS =====================
        builder.HasMany(e => e.ChatSessions)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.UploadedDocuments)
            .WithOne(e => e.UploadedByUser)
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Payments)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.QuizResults)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Explicitly defining the new structural link for QuizSessions if applicable
        builder.HasMany(e => e.QuizSessions)
            .WithOne() // Adjust .WithOne(q => q.User) if QuizSession has a navigation property back to User
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.DailyUsageLogs)
            .WithOne() // Adjust .WithOne(d => d.User) if DailyUsageLog has a navigation property back to User
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
