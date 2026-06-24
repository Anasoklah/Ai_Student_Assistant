using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Common.Extensions;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "StudentOnly")]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    IUsageTrackingService usageTrackingService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not authenticated" });

        usageTrackingService.ResetMessageCounterIfNeeded(user);
        await userManager.UpdateAsync(user);

        return Ok(ProfileMappers.MapProfile(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequestDto request)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not authenticated" });

        if (request.FullName is not null)
            user.FullName = request.FullName.Trim();
        if (request.PhoneNumber is not null)
            user.PhoneNumber = request.PhoneNumber.Trim();
        if (request.GradeLevel.HasValue)
            user.GradeLevel = request.GradeLevel;
        if (request.PreferredLanguage is not null)
            user.PreferredLanguage = request.PreferredLanguage.Trim();

        await userManager.UpdateAsync(user);
        return Ok(ProfileMappers.MapProfile(user));
    }
}
