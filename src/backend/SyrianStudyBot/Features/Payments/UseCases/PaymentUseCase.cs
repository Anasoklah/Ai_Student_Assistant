using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Features.Payments.Mappers;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Payments.Dtos;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Features.Payments.UseCases;

/// <summary>
/// Orchestrates payment operations: creating payments, submitting proof,
/// reviewing payments, and upgrading user subscriptions.
/// Relies on IPaymentRepository for all database operations.
/// </summary>
public class PaymentUseCase : IPaymentUseCase
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentUseCase(IPaymentRepository paymentRepo, UserManager<ApplicationUser> userManager)
    {
        _paymentRepo = paymentRepo;
        _userManager = userManager;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var payment = new Payment
        {
            UserId = userId,
            TargetTier = request.TargetTier,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
            Method = PaymentMethod.ShamCash,
            Status = PaymentStatus.Pending
        };

        _paymentRepo.Add(payment);
        await _paymentRepo.SaveChangesAsync(cancellationToken);

        return PaymentMappers.MapPayment(payment);
    }

    public async Task<PaymentResponseDto> SubmitProofAsync(Guid userId, Guid paymentId, SubmitPaymentProofRequestDto request, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId, userId, cancellationToken);

        if (payment is null)
            return null!;

        payment.ProviderTransactionId = request.ProviderTransactionId.Trim();
        payment.ProviderResponse = request.ProviderResponse;
        payment.Status = PaymentStatus.UnderReview;

        await _paymentRepo.SaveChangesAsync(cancellationToken);
        return PaymentMappers.MapPayment(payment);
    }

    public async Task<PagedResponse<PaymentResponseDto>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var entityPage = await _paymentRepo.GetUserPaymentsAsync(userId, page, pageSize, cancellationToken);

        return new PagedResponse<PaymentResponseDto>(
            entityPage.Items.Select(p => PaymentMappers.MapPayment(p)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    public async Task<PagedResponse<PaymentResponseDto>> GetPaymentsForAdminAsync(PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var entityPage = status.HasValue
            ? await _paymentRepo.GetPaymentsByStatusAsync(status.Value, page, pageSize, cancellationToken)
            : await _paymentRepo.GetUserPaymentsAsync(Guid.Empty, page, pageSize, cancellationToken);

        return new PagedResponse<PaymentResponseDto>(
            entityPage.Items.Select(p => PaymentMappers.MapPayment(p)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    public async Task<PaymentResponseDto> ReviewPaymentAsync(Guid paymentId, ReviewPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepo.GetByIdWithUserAsync(paymentId, cancellationToken);

        if (payment is null)
            return null!;

        if (!string.IsNullOrWhiteSpace(request.Note))
            payment.ProviderResponse = request.Note;

        if (!request.Approve)
        {
            payment.Status = PaymentStatus.Failed;
            await _paymentRepo.SaveChangesAsync(cancellationToken);
            return PaymentMappers.MapPayment(payment);
        }

        payment.Status = PaymentStatus.Completed;
        payment.CompletedAt = DateTime.UtcNow;

        if (payment.User is not null)
        {
            payment.User.SubscriptionTier = payment.TargetTier;
            payment.User.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(Math.Max(1, request.SubscriptionDays));
            await _userManager.UpdateAsync(payment.User);
        }

        await _paymentRepo.SaveChangesAsync(cancellationToken);
        return PaymentMappers.MapPayment(payment);
    }
}
