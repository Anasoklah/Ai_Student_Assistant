using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Auth.Dtos.RefreshToken;

namespace SyrianStudyBot.Features.Auth.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string? ipAddress = null, int? expiresInDays = null);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    Task<TokenResponse?> RefreshTokensAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeTokenAsync(string token, string? reason = null, string? ipAddress = null);
    Task<bool> RevokeAllUserTokensAsync(Guid userId, string? reason = null);
    Task<int> CleanupExpiredTokensAsync();
    Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(Guid userId);
}
