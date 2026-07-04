using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public SubscriptionTier TargetTier { get; set; }
    public decimal Amount { get; set; }  // In USD or equivalent
    public string Currency { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Provider-specific IDs
    public string? ProviderTransactionId { get; set; }
    public string? ProviderResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}