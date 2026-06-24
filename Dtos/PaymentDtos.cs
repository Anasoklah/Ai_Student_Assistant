using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Dtos;

public class CreatePaymentRequestDto
{
    public SubscriptionTier TargetTier { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
}

public class SubmitPaymentProofRequestDto
{
    public string ProviderTransactionId { get; init; } = string.Empty;
    public string? ProviderResponse { get; init; }
}

public class ReviewPaymentRequestDto
{
    public bool Approve { get; init; }
    public string? Note { get; init; }
    public int SubscriptionDays { get; init; } = 30;
}

public class PaymentResponseDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public SubscriptionTier TargetTier { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ProviderTransactionId { get; init; }
    public string? ProviderResponse { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
