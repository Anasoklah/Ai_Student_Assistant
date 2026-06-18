using System;

namespace SyrianStudyBot.Dtos;

public class SessionSubjectRequestDto
{
    public long UserId { get; init; }
    public string? Subject { get; init; }
}

public class SessionResponseDto
{
    public long UserId { get; init; }
    public string? CurrentSubject { get; init; }
    public string CurrentMode { get; init; } = string.Empty;
    public DateTime LastActive { get; init; }
}
