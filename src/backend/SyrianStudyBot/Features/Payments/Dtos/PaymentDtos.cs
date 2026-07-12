using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Payments.Dtos;

/// <summary>
/// Request to initiate a new payment for a subscription tier upgrade.
/// </summary>
public class CreatePaymentRequestDto
{
    /// <summary>The subscription tier the user wants to upgrade to.</summary>
    public SubscriptionTier TargetTier { get; init; }

    /// <summary>Payment amount in the specified currency.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO currency code (default "USD").</summary>
    public string Currency { get; init; } = "USD";
}

/// <summary>
/// Request to submit proof of payment (e.g., bank transfer receipt).
/// </summary>
public class SubmitPaymentProofRequestDto
{
    /// <summary>Transaction ID from the payment provider, or a receipt reference.</summary>
    public string ProviderTransactionId { get; init; } = string.Empty;

    /// <summary>Optional additional response data from the payment provider.</summary>
    public string? ProviderResponse { get; init; }
}

/// <summary>
/// Admin request to approve or reject a pending payment submission.
/// </summary>
public class ReviewPaymentRequestDto
{
    /// <summary>True to approve the payment, false to reject it.</summary>
    public bool Approve { get; init; }

    /// <summary>Optional note from the admin (e.g., rejection reason).</summary>
    public string? Note { get; init; }

    /// <summary>Number of subscription days to grant upon approval (default 30).</summary>
    public int SubscriptionDays { get; init; } = 30;
}

/// <summary>
/// Response representing a payment record with its current status and metadata.
/// </summary>
public class PaymentResponseDto
{
    /// <summary>Unique identifier of the payment record.</summary>
    public Guid Id { get; init; }

    /// <summary>ID of the user who initiated the payment.</summary>
    public Guid UserId { get; init; }

    /// <summary>The subscription tier the payment targets.</summary>
    public SubscriptionTier TargetTier { get; init; }

    /// <summary>Payment amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO currency code.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Payment method used (e.g., card, bank transfer).</summary>
    public PaymentMethod Method { get; init; }

    /// <summary>Current status of the payment (pending, completed, rejected, etc.).</summary>
    public PaymentStatus Status { get; init; }

    /// <summary>Transaction ID from the payment provider, or null if not submitted yet.</summary>
    public string? ProviderTransactionId { get; init; }

    /// <summary>Raw response data from the payment provider, or null.</summary>
    public string? ProviderResponse { get; init; }

    /// <summary>UTC timestamp when the payment record was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>UTC timestamp when the payment was completed, or null if pending/rejected.</summary>
    public DateTime? CompletedAt { get; init; }
}
