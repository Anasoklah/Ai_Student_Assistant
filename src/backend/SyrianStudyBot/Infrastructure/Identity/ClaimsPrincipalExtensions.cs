using System.Security.Claims;

namespace SyrianStudyBot.Infrastructure.Identity;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }
}
