using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyrianStudyBot.Dtos.auth;

public record ResendVerificationDto
{
    public string Email { get; set; } = string.Empty;
}
