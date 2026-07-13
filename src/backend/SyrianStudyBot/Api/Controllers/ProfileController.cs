using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.Profile;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Profile.Mappers;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Application.Profile.Dtos;

namespace SyrianStudyBot.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "StudentOnly")]
public class ProfileController(
    IProfileUseCase profileUseCase,
    UserManager<ApplicationUser> userManager) : ControllerBase
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

        var profile = await profileUseCase.GetMeAsync(user);
        return Ok(profile);
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

        var profile = await profileUseCase.UpdateMeAsync(user, request);
        return Ok(profile);
    }
}
