
using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    
    
    // ── Profile ──
    public string? FullName { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string? PreferredLanguage { get; set; } = "ar";
    
    // ── Subscription ──
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiresAt { get; set; }
    
    // ── Current Usage (for quick rate limit checks) ──
    public int MessagesToday { get; set; } = 0;
    public DateTime LastMessageReset { get; set; } = DateTime.UtcNow.Date;
    public int UploadsThisMonth { get; set; } = 0;
    public DateTime LastUploadReset { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // ── Navigation ──
    public List<ChatSession> ChatSessions { get; set; } = [];
    public List<Document> UploadedDocuments { get; set; } = [];
    public List<QuizSession> QuizSessions { get; set; } = [];
    public List<QuizResult> QuizResults { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<DailyUsageLog> DailyUsageLogs { get; set; } = [];  
    public List<RefreshToken> RefreshTokens {get;set;} = [];
}