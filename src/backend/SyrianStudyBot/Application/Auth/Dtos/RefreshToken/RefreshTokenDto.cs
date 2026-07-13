namespace SyrianStudyBot.Application.Auth.Dtos.RefreshToken;

public class RefreshTokenDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsReplaced { get; set; }
    public string? RevocationReason { get; set; }
    public bool IsActive { get; set; }
}
