using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Identity;

public class UserContextService : IUserContextService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserContextService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Guid GetCurrentUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.GetUserId();
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var userId = GetCurrentUserId(user);
        if (userId == Guid.Empty)
            return null;

        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<bool> IsUserAuthenticatedAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var currentUser = await GetCurrentUserAsync(user);
        return currentUser != null;
    }
}
