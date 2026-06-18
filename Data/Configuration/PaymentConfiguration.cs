using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyrianStudyBot.Domain;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ProviderTransactionId);

        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.ProviderTransactionId).HasMaxLength(500);
        builder.Property(e => e.ProviderResponse).HasMaxLength(2000);
    }
}