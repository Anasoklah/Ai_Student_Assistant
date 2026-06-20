using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyrianStudyBot.Dtos.auth.RefreshToken;


public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public DateTime RefreshTokenExpiry { get; init; }
}