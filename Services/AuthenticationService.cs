using System.Security.Cryptography;
using System.Text;
using Authentication.Dtos;
using Authentication.Dtos.Register;
using Authentication.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Dtos.auth.RefreshToken;

namespace Authentication.Services;

public class AuthenticationService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : IAuthenticationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IEmailService _emailService = emailService;
    private readonly AppDbContext _context = context;

    // Refresh token settings
    private int RefreshTokenExpiresInDays => _configuration.GetValue<int>("Jwt:RefreshTokenExpiresInDays", 7);
    private int MaxActiveTokensPerUser => _configuration.GetValue<int>("Jwt:MaxActiveTokensPerUser", 5);
    private int AccessTokenExpirationMinutes => _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

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

        // 3- generate access token
        var token = _jwtService.GenerateToken(user);

        // 4- generate refresh token
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        // 5- return response
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
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));

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

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = await CreateRefreshTokenAsync(userId);

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

        // Revoke all tokens when password is reset (security measure)
        await RevokeAllUserTokensAsync(userId);

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

        // Revoke all tokens when password is changed (security measure)
        await RevokeAllUserTokensAsync(userId);

        return new AuthResponse { isSuccess = true, Message = "Password Changed Successfully" };
    }

    #region Refresh Token Methods

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return new AuthResponse { isSuccess = false, Message = "Refresh token is required" };

        // Find the refresh token
        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken is null)
            return new AuthResponse { isSuccess = false, Message = "Invalid refresh token" };

        // Check if token is active
        if (!existingToken.IsActive)
        {
            // Token reuse attack detection!
            if (existingToken.IsReplaced)
            {
                // Someone is using an old replaced token - possible theft!
                await RevokeAllUserTokensAsync(existingToken.UserId);
                return new AuthResponse
                {
                    isSuccess = false,
                    Message = "Security alert: Suspicious activity detected. Please login again."
                };
            }

            return new AuthResponse { isSuccess = false, Message = "Refresh token is expired or revoked" };
        }

        var user = existingToken.User;

        // Check if user is still valid
        if (!await _userManager.IsEmailConfirmedAsync(user))
            return new AuthResponse { isSuccess = false, Message = "Email not confirmed" };

        // Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
            return new AuthResponse { isSuccess = false, Message = "Account is locked. Please contact support." };

        // Update the old token - mark as replaced (rotation)
        existingToken.IsReplaced = true;
        existingToken.LastUsedAt = DateTime.UtcNow;

        // Create new refresh token
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        // Generate new access token
        var accessToken = _jwtService.GenerateToken(user);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            isSuccess = true,
            UserName = user.FullName,
            userId = user.Id,
            Email = user.Email,
            AccessToken = accessToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiry = newRefreshToken.ExpiresAt
        };
    }

    public async Task<bool> RevokeTokenAsync(RevokeTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return false;

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (refreshToken is null)
            return false;

        if (refreshToken.IsRevoked)
            return true; // Already revoked

        refreshToken.IsRevoked = true;
        refreshToken.RevocationReason = request.Reason ?? "User logout";
        refreshToken.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevocationReason = "All tokens revoked";
            token.LastUsedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<RefreshTokenDto>> GetUserSessionsAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new RefreshTokenDto
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
            })
            .ToListAsync();

        return tokens;
    }

    #endregion

    #region Private Helper Methods

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        // Enforce max tokens limit before creating new one
        await EnforceMaxTokensLimitAsync(userId);

        // Generate cryptographically secure random token
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);

        var ipAddress = GetClientIpAddress();

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiresInDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private async Task EnforceMaxTokensLimitAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();

        if (activeTokens.Count >= MaxActiveTokensPerUser)
        {
            var tokensToRevoke = activeTokens
                .Skip(MaxActiveTokensPerUser - 1)
                .ToList();

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
                token.RevocationReason = "Exceeded maximum active tokens limit";
            }
        }
    }

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