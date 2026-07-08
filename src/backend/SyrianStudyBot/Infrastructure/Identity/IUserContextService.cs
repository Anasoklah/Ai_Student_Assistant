using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Identity;

public interface IUserContextService
{
    Guid GetCurrentUserId();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<bool> IsUserAuthenticatedAsync();
}
