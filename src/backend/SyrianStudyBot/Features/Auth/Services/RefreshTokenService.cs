using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Auth.Dtos.RefreshToken;
using SyrianStudyBot.Features.Auth.Services.Options;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Features.Auth.Services;

/// <summary>
/// Handles the full refresh token lifecycle: creation, validation,
/// rotation, revocation, and cleanup. All database operations go through
/// IRefreshTokenRepository.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IJwtService _jwtService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenService(
        IRefreshTokenRepository tokenRepo,
        IJwtService jwtService,
        UserManager<ApplicationUser> userManager,
        ILogger<RefreshTokenService> logger,
        IConfiguration configuration)
    {
        _tokenRepo = tokenRepo;
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

        await EnforceMaxTokensLimitAsync(userId);

        _tokenRepo.Add(refreshToken);
        await _tokenRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Created refresh token {TokenId} for user {UserId} from IP {IP}",
            refreshToken.Id, userId, ipAddress ?? "unknown");

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var refreshToken = await _tokenRepo.GetByTokenWithUserAsync(token);

        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found in database");
            return null;
        }

        if (!IsTokenActive(refreshToken))
        {
            _logger.LogWarning(
                "Refresh token {TokenId} is not active. Revoked: {Revoked}, Replaced: {Replaced}, Expired: {Expired}",
                refreshToken.Id, refreshToken.IsRevoked, refreshToken.IsReplaced, refreshToken.IsExpired);

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
        var existingToken = await ValidateRefreshTokenAsync(refreshToken);

        if (existingToken == null)
            return null;

        existingToken.LastUsedAt = DateTime.UtcNow;

        existingToken.IsReplaced = true;
        await _tokenRepo.SaveChangesAsync();

        var newRefreshToken = await CreateRefreshTokenAsync(
            existingToken.UserId,
            ipAddress);

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
        var refreshToken = await _tokenRepo.GetByTokenAsync(token);

        if (refreshToken == null)
            return false;

        if (refreshToken.IsRevoked)
            return true;

        refreshToken.IsRevoked = true;
        refreshToken.RevocationReason = reason;
        refreshToken.LastUsedAt = DateTime.UtcNow;

        await _tokenRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked refresh token {TokenId} for user {UserId}. Reason: {Reason}",
            refreshToken.Id, refreshToken.UserId, reason ?? "Not specified");

        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string? reason = null)
    {
        var activeTokens = await _tokenRepo.GetUserActiveTokensAsync(userId);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevocationReason = reason ?? "All tokens revoked";
            token.LastUsedAt = DateTime.UtcNow;
        }

        await _tokenRepo.SaveChangesAsync();

        _logger.LogWarning(
            "Revoked {Count} active refresh tokens for user {UserId}. Reason: {Reason}",
            activeTokens.Count, userId, reason ?? "Not specified");

        return true;
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var oldExpiredCutoff = DateTime.UtcNow.AddDays(-30);

        await _tokenRepo.BulkDeleteExpiredAsync(cutoffDate, oldExpiredCutoff);

        _logger.LogInformation("Cleaned up old refresh tokens");
        return 0; // ExecuteDeleteAsync returns count but we don't need it here
    }

    public async Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(Guid userId)
    {
        return await _tokenRepo.GetUserTokensAsync(userId);
    }

    private async Task EnforceMaxTokensLimitAsync(Guid userId)
    {
        var activeCount = await _tokenRepo.CountUserActiveTokensAsync(userId);

        if (activeCount < _settings.MaxActiveTokensPerUser)
            return;

        var tokenIdsToRevoke = await _tokenRepo.GetOldestActiveTokenIdsAsync(
            userId,
            activeCount - _settings.MaxActiveTokensPerUser + 1);

        await _tokenRepo.BulkRevokeTokensAsync(tokenIdsToRevoke);
    }

    private static bool IsTokenActive(RefreshToken token) =>
        !token.IsRevoked && !token.IsReplaced && token.ExpiresAt > DateTime.UtcNow;
}
