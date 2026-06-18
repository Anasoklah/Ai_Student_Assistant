using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain;

public class User
{
    public Guid Id { get; set; }

    // ── Basic Info (managed by AuthProject, read-only here) ──
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ── Profile (SyrianStudyBot manages) ──
    public GradeLevel? GradeLevel { get; set; }
    public string? PreferredLanguage { get; set; } = "ar";

    // ── Subscription (SyrianStudyBot manages) ──
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiresAt { get; set; }

    // ── Usage Tracking (SyrianStudyBot manages, for rate limiting) ──
    public int MessagesToday { get; set; } = 0;
    public DateTime LastMessageReset { get; set; } = DateTime.UtcNow.Date;
    public int UploadsThisMonth { get; set; } = 0;
    public DateTime LastUploadReset { get; set; } = DateTime.UtcNow;

    // ── Navigation (SyrianStudyBot features) ──
    public List<ChatSession> ChatSessions { get; set; } = [];
    public List<Document> UploadedDocuments { get; set; } = [];
    public List<QuizSession> QuizSessions { get; set; } = [];
    public List<QuizResult> QuizResults { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<DailyUsageLog> DailyUsageLogs { get; set; } = [];
}