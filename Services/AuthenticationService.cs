using System.Text;
using Authentication.Dtos;
using Authentication.Dtos.Register;
using Authentication.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Dtos.auth.RefreshToken;
using SyrianStudyBot.interfaces.Auth;

namespace Authentication.Services;

public class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IEmailService emailService,
    IRefreshTokenService refreshTokenService,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : IAuthenticationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IEmailService _emailService = emailService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;

    private int AccessTokenExpirationMinutes => _jwtService.GetAccessTokenExpirationMinutes();

    public async Task<AuthResponse> LoginAsync(LoginRequestDto request)
    {
        // 1- check if email exist and password true 
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return new AuthResponse { isSuccess = false, Message = "Invalid Email or Password" };

        // 2- check if email Confirmed 
        var isConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (!isConfirmed)
            return new AuthResponse
            {
                isSuccess = false,
                Message = "Email not confirmed, Please Confirm your Email before Login"
            };

        var token = await _jwtService.GenerateToken(user);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, GetClientIpAddress());

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new AuthResponse
        {
            isSuccess = true,
            UserName = user.FullName,
            userId = user.Id,
            Email = user.Email,
            AccessToken = token,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiresAt
        };
    }

    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null) throw new InvalidOperationException("user already exists");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = $"{request.firstName} {request.lastName}",
            PhoneNumber = request.phoneNumber,
            EmailConfirmed = false,
            PreferredLanguage = "ar"
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "Student");

        try
        {
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await _userManager.GenerateEmailConfirmationTokenAsync(user)));
            var verificationLink = $"{_configuration["AppUrl"]}/api/auth/verifyEmail?token={token}&userId={user.Id}";
            await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

            return "Registration successful! We've sent a confirmation link to your email. Please check your inbox (and spam folder) to activate your account.";
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            throw new Exception($"Failed to send verification email, please try again, {ex.Message}");
        }
    }

    public async Task<AuthResponse> VerifyEmail(Guid userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) throw new ArgumentNullException(nameof(user), "User not found");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                isSuccess = false,
                Message = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description))
            };
        }

        var accessToken = await _jwtService.GenerateToken(user);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userId, GetClientIpAddress());

        return new AuthResponse
        {
            userId = userId,
            Email = user.Email,
            isSuccess = true,
            AccessToken = accessToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiresAt,
            UserName = user.FullName
        };
    }

    public async Task<AuthResponse> ResentEmailVerification(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new AuthResponse
            {
                isSuccess = false,
                Message = "If the account exists, a verification email has been sent."
            };

        var isConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (isConfirmed)
            return new AuthResponse
            {
                isSuccess = false,
                Message = "Email Already Confirmed, You Can Login"
            };

        try
        {
            var token = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(
                    await _userManager.GenerateEmailConfirmationTokenAsync(user)));

            var verificationLink = $"{_configuration["AppUrl"]}/api/auth/verifyEmail?token={token}&userId={user.Id}";
            await _emailService.SendVerificationEmailAsync(user.Email!, verificationLink);

            return new AuthResponse
            {
                isSuccess = true,
                Message = "Email Confirmation Resent Successfully, Please Check Your Email"
            };
        }
        catch (Exception)
        {
            return new AuthResponse
            {
                isSuccess = false,
                Message = "Failed to send verification email. Please try again later."
            };
        }
    }

    public async Task<AuthResponse> ForgetPassword(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new AuthResponse { isSuccess = false, Message = "If user registered and confirmed, email sent" };

        var isConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (!isConfirmed)
            return new AuthResponse { isSuccess = false, Message = "Please Confirm your Email first" };

        var token = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(await _userManager.GeneratePasswordResetTokenAsync(user)));

        var resetLink = $"{_configuration["AppUrl"]}/api/auth/ResetPassword?token={token}&userId={user.Id}";

        try
        {
            await _emailService.SendResetPasswordToken(user.Email!, resetLink);
            return new AuthResponse { isSuccess = true, Message = "Please check your email" };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send email, please try again, {ex.Message}");
        }
    }

    public async Task<AuthResponse> ResetPassword(Guid userId, string newPassword, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new AuthResponse { isSuccess = false, Message = "User not found" };

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

        if (!result.Succeeded)
            return new AuthResponse
            {
                isSuccess = false,
                Message = string.Join(",", result.Errors.Select(e => e.Description))
            };

        await _refreshTokenService.RevokeAllUserTokensAsync(userId, "Password reset");

        return new AuthResponse { isSuccess = true, Message = "Password changed, you can login now" };
    }

    public async Task<AuthResponse> ChangePassword(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new AuthResponse { isSuccess = false, Message = "Error while changing password, please try again later" };

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        if (!result.Succeeded)
            return new AuthResponse
            {
                isSuccess = false,
                Message = string.Join(",", result.Errors.Select(x => x.Description))
            };

        await _refreshTokenService.RevokeAllUserTokensAsync(userId, "Password changed");

        return new AuthResponse { isSuccess = true, Message = "Password Changed Successfully" };
    }

    #region Refresh Token Methods

  public async Task<AuthResponse> RefreshTokenAsync(RefreshRequestDto request)
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
        return new AuthResponse { isSuccess = false, Message = "Refresh token is required" };

    var tokenResponse = await _refreshTokenService.RefreshTokensAsync(
        request.RefreshToken, GetClientIpAddress());
        
    if (tokenResponse is null)
        return new AuthResponse { isSuccess = false, Message = "Invalid refresh token" };

    return new AuthResponse
    {
        isSuccess = true,
        UserName = tokenResponse.UserName,
        userId = tokenResponse.UserId,
        Email = tokenResponse.Email,
        AccessToken = tokenResponse.AccessToken,
        AccessTokenExpiry = tokenResponse.AccessTokenExpiry,
        RefreshToken = tokenResponse.RefreshToken,
        RefreshTokenExpiry = tokenResponse.RefreshTokenExpiry
    };
}

    public async Task<bool> RevokeTokenAsync(RevokeTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return false;

        return await _refreshTokenService.RevokeTokenAsync(request.RefreshToken, request.Reason ?? "User logout", GetClientIpAddress());
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        return await _refreshTokenService.RevokeAllUserTokensAsync(userId, "All tokens revoked");
    }

    public async Task<IEnumerable<RefreshTokenDto>> GetUserSessionsAsync(Guid userId)
    {
        var tokens = await _refreshTokenService.GetUserRefreshTokensAsync(userId);
        return tokens.Select(rt => new RefreshTokenDto
        {
            Id = rt.Id,
            CreatedAt = rt.CreatedAt,
            ExpiresAt = rt.ExpiresAt,
            LastUsedAt = rt.LastUsedAt,
            CreatedByIp = rt.CreatedByIp,
            IsRevoked = rt.IsRevoked,
            IsReplaced = rt.IsReplaced,
            RevocationReason = rt.RevocationReason,
            IsActive = rt.IsActive
        });
    }

    #endregion

    #region Private Helper Methods

    private string? GetClientIpAddress()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return null;

        // Check for proxy headers first
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstIp = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
