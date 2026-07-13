using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Payments.Dtos;

namespace SyrianStudyBot.Application.Payments.Mappers;

public static class PaymentMappers
{
    public static PaymentResponseDto MapPayment(Payment payment) => new()
    {
        Id = payment.Id,
        UserId = payment.UserId,
        TargetTier = payment.TargetTier,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Method = payment.Method,
        Status = payment.Status,
        ProviderTransactionId = payment.ProviderTransactionId,
        ProviderResponse = payment.ProviderResponse,
        CreatedAt = payment.CreatedAt,
        CompletedAt = payment.CompletedAt
    };
}
