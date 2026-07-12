using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Features.contracts.repositories;

/// <summary>
/// Repository for all RefreshToken database operations.
/// Handles token lifecycle: creation, validation, revocation, cleanup.
///
/// Replaces direct AppDbContext usage in RefreshTokenService.
/// </summary>
public interface IRefreshTokenRepository
{
    // ── RefreshToken queries ──

    /// <summary>
    /// Returns a refresh token by its token string value.
    /// Returns null if not found.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Returns a refresh token by token string, including the related User entity.
    /// Used when we need to access token.User during refresh operations.
    /// </summary>
    Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Returns all active (non-revoked, non-replaced, non-expired) refresh tokens for a user.
    /// Used to determine how many sessions a user has open.
    /// </summary>
    Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of active (non-revoked, non-replaced, non-expired) refresh tokens.
    /// Used to enforce session limits before creating new tokens.
    /// </summary>
    Task<int> CountUserActiveTokensAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the IDs of the oldest active tokens to revoke when exceeding session limits.
    /// </summary>
    Task<List<Guid>> GetOldestActiveTokenIdsAsync(Guid userId, int count, CancellationToken ct = default);

    /// <summary>
    /// Returns all refresh tokens (active and revoked) for a user, ordered by most recent.
    /// Used for displaying session history.
    /// </summary>
    Task<List<RefreshToken>> GetUserTokensAsync(Guid userId, CancellationToken ct = default);

    // ── RefreshToken commands ──

    /// <summary>
    /// Stages a new RefreshToken for insertion.
    /// </summary>
    void Add(RefreshToken token);

    /// <summary>
    /// Bulk-revokes a list of refresh tokens by setting IsRevoked = true.
    /// Used when enforcing session limits (revoke oldest sessions).
    /// </summary>
    Task BulkRevokeTokensAsync(List<Guid> tokenIds, CancellationToken ct = default);

    /// <summary>
    /// Bulk-deletes expired and old revoked refresh tokens for cleanup.
    /// Removes revoked tokens older than cutoffDate AND expired tokens older than olderThan.
    /// </summary>
    Task BulkDeleteExpiredAsync(DateTime cutoffDate, DateTime olderThan, CancellationToken ct = default);

    // ── Unit of Work ──

    /// <summary>
    /// Persists all tracked entity changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
