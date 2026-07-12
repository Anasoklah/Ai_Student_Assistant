using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Infrastructure.Persistence.Repositories;

namespace SyrianStudyBot.Features.contracts.repositories;

/// <summary>
/// Repository for all Chat-related database operations.
/// Covers: ChatSession, ChatMessage.
///
/// Replaces direct AppDbContext usage in ChatUseCase.
/// </summary>
public interface IChatRepository
{
    // ── ChatSession queries ──

    /// <summary>
    /// Returns a paginated list of chat sessions for a user.
    /// Ordered by most recently active first.
    /// </summary>
    Task<EntityPage<ChatSession>> GetUserSessionsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a single chat session if it exists and belongs to the user.
    /// Returns null if not found.
    /// </summary>
    Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if a chat session exists and belongs to the user.
    /// </summary>
    Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    // ── ChatSession commands ──

    /// <summary>
    /// Stages a new ChatSession for insertion.
    /// </summary>
    void AddSession(ChatSession session);

    // ── ChatMessage queries ──

    /// <summary>
    /// Returns a paginated list of messages for a session.
    /// Ordered by timestamp (oldest first).
    /// </summary>
    Task<EntityPage<ChatMessage>> GetSessionMessagesAsync(Guid sessionId, int page, int pageSize, CancellationToken ct = default);

    // ── ChatMessage commands ──

    /// <summary>
    /// Stages a new ChatMessage for insertion.
    /// </summary>
    void AddMessage(ChatMessage message);

    // ── Unit of Work ──

    /// <summary>
    /// Persists all tracked entity changes. Since repositories share the same
    /// scoped DbContext, this also persists changes from other repositories
    /// (e.g., UsageTrackingService).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
