using SyrianStudyBot.Features.Auth.Services;
using SyrianStudyBot.Features.Auth.Dtos;
using SyrianStudyBot.Features.Auth.Dtos.RefreshToken;

namespace SyrianStudyBot.Features.Auth.UseCases;

public class AuthUseCase : IAuthUseCase
{
    private readonly IAuthenticationService _authService;

    public AuthUseCase(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<string> RegisterAsync(RegisterRequest request)
        => await _authService.RegisterAsync(request);

    public async Task<AuthResponse> LoginAsync(LoginRequestDto request)
        => await _authService.LoginAsync(request);

    public async Task<AuthResponse> VerifyEmail(Guid userId, string token)
        => await _authService.VerifyEmail(userId, token);

    public async Task<AuthResponse> ResentEmailVerification(string email)
        => await _authService.ResentEmailVerification(email);

    public async Task<AuthResponse> ForgetPassword(string email)
        => await _authService.ForgetPassword(email);

    public async Task<AuthResponse> ResetPassword(Guid userId, string newPassword, string token)
        => await _authService.ResetPassword(userId, newPassword, token);

    public async Task<AuthResponse> ChangePassword(Guid userId, string oldPassword, string newPassword)
        => await _authService.ChangePassword(userId, oldPassword, newPassword);

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequestDto request)
        => await _authService.RefreshTokenAsync(request);

    public async Task<bool> RevokeTokenAsync(RevokeTokenRequestDto request)
        => await _authService.RevokeTokenAsync(request);

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
        => await _authService.RevokeAllUserTokensAsync(userId);

    public async Task<IEnumerable<RefreshTokenDto>> GetUserSessionsAsync(Guid userId)
        => await _authService.GetUserSessionsAsync(userId);
}
