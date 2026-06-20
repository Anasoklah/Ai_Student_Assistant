
using SyrianStudyBot.Domain;

namespace Authentication.interfaces;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user);
    int GetAccessTokenExpirationMinutes();
}
