using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;

public class DailyUsageLogConfiguration : IEntityTypeConfiguration<DailyUsageLog>
{
    public void Configure(EntityTypeBuilder<DailyUsageLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        builder.HasIndex(e => e.Date);

        builder.Property(e => e.EstimatedCost).HasPrecision(18, 6);

        builder.HasOne(e => e.User)
            .WithMany(e => e.DailyUsageLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
