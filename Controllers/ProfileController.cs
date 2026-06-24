using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "StudentOnly")]
public class ProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not authenticated" });

        ResetUsageCountersIfNeeded(user);
        await userManager.UpdateAsync(user);

        return Ok(MapProfile(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequestDto request)
    {
        var user = await GetCurrentUserAsync();
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
        return Ok(MapProfile(user));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.GetUserId();
        return userId == Guid.Empty ? null : await userManager.FindByIdAsync(userId.ToString());
    }

    private static ProfileResponseDto MapProfile(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        GradeLevel = user.GradeLevel,
        PreferredLanguage = user.PreferredLanguage,
        SubscriptionTier = user.SubscriptionTier,
        SubscriptionExpiresAt = user.SubscriptionExpiresAt,
        MessagesToday = user.MessagesToday,
        DailyMessageLimit = GetDailyMessageLimit(user.SubscriptionTier),
        UploadsThisMonth = user.UploadsThisMonth,
        MonthlyUploadLimit = GetMonthlyUploadLimit(user.SubscriptionTier)
    };

    private static void ResetUsageCountersIfNeeded(ApplicationUser user)
    {
        var today = DateTime.UtcNow.Date;
        if (user.LastMessageReset.Date < today)
        {
            user.MessagesToday = 0;
            user.LastMessageReset = today;
        }

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        if (user.LastUploadReset < currentMonth)
        {
            user.UploadsThisMonth = 0;
            user.LastUploadReset = DateTime.UtcNow;
        }
    }

    private static int GetDailyMessageLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 10,
        SubscriptionTier.Pro => 500,
        SubscriptionTier.Ultra => 2000,
        _ => 10
    };

    private static int GetMonthlyUploadLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 0,
        SubscriptionTier.Pro => 10,
        SubscriptionTier.Ultra => 100,
        _ => 0
    };
}
