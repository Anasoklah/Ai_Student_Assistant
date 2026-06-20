using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Dtos.auth.RefreshToken;

namespace SyrianStudyBot.interfaces.Auth;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user
    /// </summary>
    Task<RefreshToken> CreateRefreshTokenAsync(
        Guid userId, 
        string? ipAddress = null,
        int? expiresInDays = null);
    
    /// <summary>
    /// Validates a refresh token and returns it if valid
    /// </summary>
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    
    /// <summary>
    /// Refreshes tokens - validates old token, creates new pair
    /// </summary>
    Task<TokenResponse?> RefreshTokensAsync(string refreshToken, string? ipAddress = null);
    
    /// <summary>
    /// Revokes a specific refresh token
    /// </summary>
    Task<bool> RevokeTokenAsync(string token, string? reason = null, string? ipAddress = null);
    
    /// <summary>
    /// Revokes ALL refresh tokens for a user (logout from all devices)
    /// </summary>
    Task<bool> RevokeAllUserTokensAsync(Guid userId, string? reason = null);
    
    /// <summary>
    /// Removes expired/revoked tokens from database (cleanup)
    /// </summary>
    Task<int> CleanupExpiredTokensAsync();
    
    /// <summary>
    /// Gets all active refresh tokens for a user
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(Guid userId);
}
