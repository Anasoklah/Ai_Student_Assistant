using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyrianStudyBot.Dtos.auth.RefreshToken;

public class RefreshRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
