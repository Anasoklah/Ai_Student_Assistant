using SyrianStudyBot.Domain;

namespace SyrianStudyBot.Common.Services;

public interface IUserContextService
{
    Guid GetCurrentUserId(System.Security.Claims.ClaimsPrincipal user);
    Task<ApplicationUser?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user);
    Task<bool> IsUserAuthenticatedAsync(System.Security.Claims.ClaimsPrincipal user);
}
