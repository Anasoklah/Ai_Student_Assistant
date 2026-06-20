

namespace SyrianStudyBot.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    
    public string Token { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
    
    // Token will expire after this date
    public DateTime ExpiresAt { get; set; }
    
    // When the token was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // When the token was last used (for refresh)
    public DateTime? LastUsedAt { get; set; }
    
    // Token status
    public bool IsRevoked { get; set; } = false;
    
    // Has this token been replaced by a new one (rotation)
    public bool IsReplaced { get; set; } = false;
    
    // IP address of the client that created the token
    public string? CreatedByIp { get; set; }
    
    // Reason for revocation (if any)
    public string? RevocationReason { get; set; }
    
    // Computed property to check if token is active
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    public bool IsActive => !IsRevoked && !IsReplaced && !IsExpired;
}
