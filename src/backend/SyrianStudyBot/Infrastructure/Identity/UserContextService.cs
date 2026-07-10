using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Identity;

public class UserContextService : IUserContextService
{
    private readonly UserManager<ApplicationUser> _userManager;
     private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public Guid GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.GetUserId() ?? Guid.Empty;
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return null;

        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<bool> IsUserAuthenticatedAsync()
    {   
        return GetCurrentUserId() == Guid.Empty ? false : true;
    }

    public async Task<bool> IsInRole(string role)
    {
        var Role = role.Trim();
        var user = await GetCurrentUserAsync();
        return await _userManager.IsInRoleAsync( user! , Role);
    }
}
