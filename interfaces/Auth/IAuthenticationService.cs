
using Authentication.Dtos;
using Authentication.Dtos.Register;
using SyrianStudyBot.Dtos.auth.RefreshToken;

namespace Authentication.interfaces;

public interface IAuthenticationService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequestDto request);
    Task<AuthResponse> VerifyEmail(Guid userId , string token );
    Task<AuthResponse> ResentEmailVerification(string email);

    Task<AuthResponse> ChangePassword(Guid userid , string oldPassword , string newPassword);

    Task<AuthResponse> ForgetPassword(string email);
    Task<AuthResponse> ResetPassword(Guid userId , string newPassword , string token);
        
        // refresh token methods
    Task<AuthResponse> RefreshTokenAsync(RefreshRequestDto request);
    Task<bool> RevokeTokenAsync(RevokeTokenRequestDto request);
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
    Task<IEnumerable<RefreshTokenDto>> GetUserSessionsAsync(Guid userId);
}
