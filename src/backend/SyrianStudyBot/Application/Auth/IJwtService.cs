using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Application.Auth;

public interface IJwtService
{
    Task<string> GenerateToken(ApplicationUser user);
    int GetAccessTokenExpirationMinutes();
}
