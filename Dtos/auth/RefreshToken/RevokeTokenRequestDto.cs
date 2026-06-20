using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyrianStudyBot.Dtos.auth.RefreshToken;

public record RevokeTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
