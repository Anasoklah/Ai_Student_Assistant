using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.Auth.Dtos;
using SyrianStudyBot.Application.Auth.Dtos.RefreshToken;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Application.Common;

namespace SyrianStudyBot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthUseCase _authUseCase;

    public AuthController(IAuthUseCase authUseCase)
    {
        _authUseCase = authUseCase;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authUseCase.RegisterAsync(request);
        return Ok(new { message = result });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authUseCase.LoginAsync(request);

        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpGet("verifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var result = await _authUseCase.VerifyEmail(userId, token);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
    {
        var result = await _authUseCase.ResentEmailVerification(request.Email);
        return Ok(new { message = result.Message });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var result = await _authUseCase.ForgetPassword(request.Email);
        return Ok(new { message = result.Message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var result = await _authUseCase.ResetPassword(request.UserId, request.NewPassword, request.Token);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = User.GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var result = await _authUseCase.ChangePassword(userId, request.OldPassword, request.NewPassword);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(new 
        { 
            message = result.Message, 
            note = "All sessions have been revoked. Please login again." 
        });
    }

    #region Refresh Token Endpoints

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var result = await _authUseCase.RefreshTokenAsync(request);

        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
    {
        var result = await _authUseCase.RevokeTokenAsync(request);

        if (!result)
            return NotFound(new { message = "Refresh token not found" });

        return Ok(new { message = "Token revoked successfully" });
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutFromAllDevices()
    {
         var userId = User.GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        await _authUseCase.RevokeAllUserTokensAsync(userId);

        return Ok(new { message = "Logged out from all devices successfully" });
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions()
    {
       var userId = User.GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var sessions = await _authUseCase.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    #endregion
}
