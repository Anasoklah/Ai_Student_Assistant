using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Authentication.Dtos;
using Authentication.Dtos.Register;
using SyrianStudyBot.Dtos.auth.RefreshToken;
using SyrianStudyBot.Dtos.auth;
using Authentication.interfaces;
using SyrianStudyBot.Common.Extensions;

namespace Authentication.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Login user - returns access token and refresh token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.isSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    [HttpGet("verifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var result = await _authService.VerifyEmail(userId, token);

        if (!result.isSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
    {
        var result = await _authService.ResentEmailVerification(request.Email);
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Forgot password - sends reset link
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var result = await _authService.ForgetPassword(request.Email);
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Reset password
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] Dtos.ResetPassword.ResetPasswordDto request)
    {
        var result = await _authService.ResetPassword(request.userid, request.newPassword, request.token);

        if (!result.isSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Change password (authenticated user)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] Dtos.ResetPassword.ChangePasswordDto request)
    {
        var userId = User.GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var result = await _authService.ChangePassword(userId, request.oldPassword, request.newPassword);

        if (!result.isSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(new 
        { 
            message = result.Message, 
            note = "All sessions have been revoked. Please login again." 
        });
    }

    #region Refresh Token Endpoints

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        if (!result.isSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Revoke a specific refresh token (logout from current device)
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
    {
        var result = await _authService.RevokeTokenAsync(request);

        if (!result)
            return NotFound(new { message = "Refresh token not found" });

        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Logout from all devices - revokes all refresh tokens
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutFromAllDevices()
    {
         Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value , out var userId);

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        await _authService.RevokeAllUserTokensAsync(userId);

        return Ok(new { message = "Logged out from all devices successfully" });
    }

    /// <summary>
    /// Get all active sessions for the authenticated user
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions()
    {
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value , out var userId);

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var sessions = await _authService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    #endregion
}