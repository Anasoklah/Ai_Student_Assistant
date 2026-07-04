using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Features.Auth.Services;

public interface IJwtService
{
    Task<string> GenerateToken(ApplicationUser user);
    int GetAccessTokenExpirationMinutes();
}
