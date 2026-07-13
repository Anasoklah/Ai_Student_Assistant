using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Application.Common;

public interface IUserContextService
{
    Guid GetCurrentUserId();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<bool> IsUserAuthenticatedAsync();

    Task<bool> IsInRole(string role);
}
