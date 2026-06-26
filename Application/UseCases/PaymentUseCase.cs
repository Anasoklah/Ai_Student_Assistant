using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Application.UseCases;

public interface IPaymentUseCase
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> SubmitProofAsync(Guid userId, Guid paymentId, SubmitPaymentProofRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetPaymentsForAdminAsync(PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> ReviewPaymentAsync(Guid paymentId, ReviewPaymentRequestDto request, CancellationToken cancellationToken = default);
}

public class PaymentUseCase : IPaymentUseCase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPagingService _pagingService;

    public PaymentUseCase(AppDbContext db, UserManager<ApplicationUser> userManager, IPagingService pagingService)
    {
        _db = db;
        _userManager = userManager;
        _pagingService = pagingService;
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

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return PaymentMappers.MapPayment(payment);
    }

    public async Task<PaymentResponseDto> SubmitProofAsync(Guid userId, Guid paymentId, SubmitPaymentProofRequestDto request, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, cancellationToken);

        if (payment is null)
            return null!;

        payment.ProviderTransactionId = request.ProviderTransactionId.Trim();
        payment.ProviderResponse = request.ProviderResponse;
        payment.Status = PaymentStatus.UnderReview;

        await _db.SaveChangesAsync(cancellationToken);
        return PaymentMappers.MapPayment(payment);
    }

    public async Task<PagedResponse<PaymentResponseDto>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var query = _db.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PaymentMappers.MapPayment(p))
            .ToListAsync(cancellationToken);

        return new PagedResponse<PaymentResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResponse<PaymentResponseDto>> GetPaymentsForAdminAsync(PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var query = _db.Payments.AsQueryable();
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PaymentMappers.MapPayment(p))
            .ToListAsync(cancellationToken);

        return new PagedResponse<PaymentResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PaymentResponseDto> ReviewPaymentAsync(Guid paymentId, ReviewPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return null!;

        if (!string.IsNullOrWhiteSpace(request.Note))
            payment.ProviderResponse = request.Note;

        if (!request.Approve)
        {
            payment.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync(cancellationToken);
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

        await _db.SaveChangesAsync(cancellationToken);
        return PaymentMappers.MapPayment(payment);
    }
}
