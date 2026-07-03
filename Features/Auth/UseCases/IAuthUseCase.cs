using SyrianStudyBot.Features.Auth.Dtos;
using SyrianStudyBot.Features.Auth.Dtos.RefreshToken;

namespace SyrianStudyBot.Features.Auth.UseCases;

public interface IAuthUseCase
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequestDto request);
    Task<AuthResponse> VerifyEmail(Guid userId, string token);
    Task<AuthResponse> ResentEmailVerification(string email);
    Task<AuthResponse> ForgetPassword(string email);
    Task<AuthResponse> ResetPassword(Guid userId, string newPassword, string token);
    Task<AuthResponse> ChangePassword(Guid userId, string oldPassword, string newPassword);
    Task<AuthResponse> RefreshTokenAsync(RefreshRequestDto request);
    Task<bool> RevokeTokenAsync(RevokeTokenRequestDto request);
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
    Task<IEnumerable<RefreshTokenDto>> GetUserSessionsAsync(Guid userId);
}
