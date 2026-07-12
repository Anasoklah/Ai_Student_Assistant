using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles all database operations for RefreshToken entities.
/// Manages the full token lifecycle: creation, validation, revocation, and cleanup.
///
/// The most complex repository in terms of operations — supports:
/// - Bulk revoke via ExecuteUpdateAsync
/// - Bulk delete via ExecuteDeleteAsync
/// - Count queries for session limit enforcement
/// - Include with related User entity
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── RefreshToken queries ──

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);
    }

    public async Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsReplaced && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<int> CountUserActiveTokensAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .CountAsync(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsReplaced && rt.ExpiresAt > DateTime.UtcNow, ct);
    }

    public async Task<List<Guid>> GetOldestActiveTokenIdsAsync(Guid userId, int count, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsReplaced && rt.ExpiresAt > DateTime.UtcNow)
            .OrderBy(rt => rt.CreatedAt)
            .Take(count)
            .Select(rt => rt.Id)
            .ToListAsync(ct);
    }

    public async Task<List<RefreshToken>> GetUserTokensAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    // ── RefreshToken commands ──

    public void Add(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
    }

    public async Task BulkRevokeTokensAsync(List<Guid> tokenIds, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(rt => tokenIds.Contains(rt.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevocationReason, "Replaced by new token"));
    }

    public async Task BulkDeleteExpiredAsync(DateTime cutoffDate, DateTime olderThan, CancellationToken ct = default)
    {
        // Delete revoked tokens older than cutoffDate
        await _db.RefreshTokens
            .Where(rt => rt.IsRevoked && rt.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync();

        // Delete expired tokens created before olderThan
        await _db.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow && rt.CreatedAt < olderThan)
            .ExecuteDeleteAsync();
    }

    // ── Unit of Work ──

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync();
    }
}
