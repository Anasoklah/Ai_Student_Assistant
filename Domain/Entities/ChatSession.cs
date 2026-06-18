using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyrianStudyBot.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Title { get; set; }  // Auto-generated from first message
    public string? Subject { get; set; }  // Filter subject for this session
    public string Mode { get; set; } = "explain";  // explain, summary, quiz

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = [];
}
