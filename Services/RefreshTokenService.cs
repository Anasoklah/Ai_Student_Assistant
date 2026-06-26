using System.Security.Cryptography;
using Authentication.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Dtos.auth.RefreshToken;
using SyrianStudyBot.interfaces.Auth;

namespace SyrianStudyBot.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenService(
        AppDbContext context,
        IJwtService jwtService,
        UserManager<ApplicationUser> userManager,
        ILogger<RefreshTokenService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _userManager = userManager;
        _logger = logger;
        
        _settings = new RefreshTokenSettings
        {
            ExpiresInDays = configuration.GetValue<int>("Jwt:RefreshTokenExpiresInDays", 7),
            MaxActiveTokensPerUser = configuration.GetValue<int>("Jwt:MaxActiveTokensPerUser", 5)
        };
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        Guid userId, 
        string? ipAddress = null,
        int? expiresInDays = null)
    {
        // Generate a secure random token
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays ?? _settings.ExpiresInDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        // Enforce maximum active tokens per user (security measure)
        await EnforceMaxTokensLimitAsync(userId);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created refresh token {TokenId} for user {UserId} from IP {IP}", 
            refreshToken.Id, userId, ipAddress ?? "unknown");

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found in database");
            return null;
        }

        // Check if token is active
        if (!IsTokenActive(refreshToken))
        {
            _logger.LogWarning(
                "Refresh token {TokenId} is not active. Revoked: {Revoked}, Replaced: {Replaced}, Expired: {Expired}",
                refreshToken.Id, refreshToken.IsRevoked, refreshToken.IsReplaced, refreshToken.IsExpired);
            
            // If a replaced token is being used, it might be a token reuse attack!
            if (refreshToken.IsReplaced)
            {
                _logger.LogWarning(
                    "Possible token reuse attack detected! Token {TokenId} was replaced but is being used again. Revoking all tokens for user {UserId}",
                    refreshToken.Id, refreshToken.UserId);
                await RevokeAllUserTokensAsync(refreshToken.UserId, "Possible token reuse attack");
            }
            
            return null;
        }

        if (!await _userManager.IsEmailConfirmedAsync(refreshToken.User))
        {
            _logger.LogWarning("Refresh token {TokenId} belongs to an unconfirmed user", refreshToken.Id);
            return null;
        }

        if (await _userManager.IsLockedOutAsync(refreshToken.User))
        {
            _logger.LogWarning("Refresh token {TokenId} belongs to a locked-out user", refreshToken.Id);
            return null;
        }

        return refreshToken;
    }

    public async Task<TokenResponse?> RefreshTokensAsync(string refreshToken, string? ipAddress = null)
    {
        // Validate the refresh token
        var existingToken = await ValidateRefreshTokenAsync(refreshToken);
        
        if (existingToken == null)
            return null;

        // Update the last used timestamp
        existingToken.LastUsedAt = DateTime.UtcNow;

        // Mark the old token as replaced (token rotation)
        existingToken.IsReplaced = true;
        await _context.SaveChangesAsync();

        // Create new refresh token (rotation)
        var newRefreshToken = await CreateRefreshTokenAsync(
            existingToken.UserId, 
            ipAddress);

        // Generate new access token
        var user = existingToken.User;
        var accessToken = await _jwtService.GenerateToken(user);
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
            _jwtService.GetAccessTokenExpirationMinutes());

        _logger.LogInformation(
            "Rotated refresh token for user {UserId}. Old: {OldTokenId}, New: {NewTokenId}",
            user.Id, existingToken.Id, newRefreshToken.Id);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiry = accessTokenExpiry,
            RefreshTokenExpiry = newRefreshToken.ExpiresAt,
            Email = user.Email,
            UserId = user.Id,
            UserName = user.FullName
        };
    }

    public async Task<bool> RevokeTokenAsync(
        string token, 
        string? reason = null, 
        string? ipAddress = null)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
            return false;

        if (refreshToken.IsRevoked)
            return true; // Already revoked

        refreshToken.IsRevoked = true;
        refreshToken.RevocationReason = reason;
        refreshToken.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked refresh token {TokenId} for user {UserId}. Reason: {Reason}",
            refreshToken.Id, refreshToken.UserId, reason ?? "Not specified");

        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string? reason = null)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId
                && !rt.IsRevoked
                && !rt.IsReplaced
                && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevocationReason = reason ?? "All tokens revoked";
            token.LastUsedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Revoked {Count} active refresh tokens for user {UserId}. Reason: {Reason}",
            activeTokens.Count, userId, reason ?? "Not specified");

        return true;
    }

     public async Task<int> CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var oldExpiredCutoff = DateTime.UtcNow.AddDays(-30);

        // Single atomic DELETE query — zero memory allocation
        var revokedDeleted = await _context.RefreshTokens
            .Where(rt => rt.IsRevoked && rt.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync();

        var expiredDeleted = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow && rt.CreatedAt < oldExpiredCutoff)
            .ExecuteDeleteAsync();

        var total = revokedDeleted + expiredDeleted;
        _logger.LogInformation("Cleaned up {Count} old refresh tokens", total);
        return total;
    }
    public async Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Ensures user doesn't have too many active tokens (security measure)
    /// </summary>
 private async Task EnforceMaxTokensLimitAsync(Guid userId)
{
    // Use ExecuteUpdate for atomic operation — no race condition
    var activeCount = await _context.RefreshTokens
        .CountAsync(rt => rt.UserId == userId
            && !rt.IsRevoked
            && !rt.IsReplaced
            && rt.ExpiresAt > DateTime.UtcNow);

    if (activeCount < _settings.MaxActiveTokensPerUser)
        return;

    // Get IDs of tokens to revoke (oldest first)
    var tokenIdsToRevoke = await _context.RefreshTokens
        .Where(rt => rt.UserId == userId
            && !rt.IsRevoked
            && !rt.IsReplaced
            && rt.ExpiresAt > DateTime.UtcNow)
        .OrderBy(rt => rt.CreatedAt)
        .Take(activeCount - _settings.MaxActiveTokensPerUser + 1)
        .Select(rt => rt.Id)
        .ToListAsync();

    // Atomic bulk update — no loading into memory
    await _context.RefreshTokens
        .Where(rt => tokenIdsToRevoke.Contains(rt.Id))
        .ExecuteUpdateAsync(s => s
            .SetProperty(rt => rt.IsRevoked, true)
            .SetProperty(rt => rt.RevocationReason, "Exceeded maximum active tokens limit"));
}

    private static bool IsTokenActive(RefreshToken token) =>
        !token.IsRevoked && !token.IsReplaced && token.ExpiresAt > DateTime.UtcNow;
}

/// <summary>
/// Settings class for refresh token configuration
/// </summary>
public class RefreshTokenSettings
{
    public int ExpiresInDays { get; set; } = 7;
    public int MaxActiveTokensPerUser { get; set; } = 5;
}
