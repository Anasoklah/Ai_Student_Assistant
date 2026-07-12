using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Infrastructure.Persistence.Repositories;

namespace SyrianStudyBot.Features.contracts.repositories;

/// <summary>
/// Repository for all Payment-related database operations.
/// Covers: Payment entity (includes eager-loading of related ApplicationUser).
///
/// Replaces direct AppDbContext usage in PaymentUseCase.
/// </summary>
public interface IPaymentRepository
{
    // ── Payment queries ──

    /// <summary>
    /// Returns a single payment by ID if it belongs to the user.
    /// Returns null if not found.
    /// </summary>
    Task<Payment?> GetByIdAsync(Guid paymentId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single payment by ID, including the related User entity.
    /// Used when we need to access payment.User.SubscriptionTier.
    /// </summary>
    Task<Payment?> GetByIdWithUserAsync(Guid paymentId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of payments for a user.
    /// Ordered by most recent first.
    /// </summary>
    Task<EntityPage<Payment>> GetUserPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of payments filtered by status.
    /// Used by admin to view pending/completed/failed payments.
    /// </summary>
    Task<EntityPage<Payment>> GetPaymentsByStatusAsync(PaymentStatus status, int page, int pageSize, CancellationToken ct = default);

    // ── Payment commands ──

    /// <summary>
    /// Stages a new Payment for insertion.
    /// </summary>
    void Add(Payment payment);

    // ── Unit of Work ──

    /// <summary>
    /// Persists all tracked entity changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
