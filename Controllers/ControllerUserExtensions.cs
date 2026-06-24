using System.Security.Claims;

namespace SyrianStudyBot.Controllers;

internal static class ControllerUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }
}
