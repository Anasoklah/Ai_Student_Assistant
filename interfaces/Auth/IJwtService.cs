
using SyrianStudyBot.Domain;

namespace Authentication.interfaces;

public interface IJwtService
{
    Task<string> GenerateToken(ApplicationUser user);
    int GetAccessTokenExpirationMinutes();
}
