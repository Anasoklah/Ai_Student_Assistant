using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles all database operations for Payment entities.
/// Includes eager-loading of related User when needed (e.g., for SubscriptionTier checks).
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── Payment queries ──

    public async Task<Payment?> GetByIdAsync(Guid paymentId, Guid userId, CancellationToken ct = default)
    {
        return await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, ct);
    }

    public async Task<Payment?> GetByIdWithUserAsync(Guid paymentId, CancellationToken ct = default)
    {
        return await _db.Payments
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
    }

    public async Task<EntityPage<Payment>> GetUserPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<EntityPage<Payment>> GetPaymentsByStatusAsync(PaymentStatus status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Payments
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    // ── Payment commands ──

    public void Add(Payment payment)
    {
        _db.Payments.Add(payment);
    }

    // ── Unit of Work ──

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ──

    private static async Task<EntityPage<T>> PaginateAsync<T>(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new EntityPage<T>(items, totalCount, page, pageSize);
    }
}
