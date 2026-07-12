using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.contracts.repositories;
using SyrianStudyBot.Infrastructure.Persistence;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles all database operations for ChatSession and ChatMessage entities.
/// All chat-related queries and commands go through this repository.
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── ChatSession queries ──

    public async Task<EntityPage<ChatSession>> GetUserSessionsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActiveAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        return await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.IsActive, ct);
    }

    public async Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        return await _db.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId, ct);
    }

    // ── ChatSession commands ──

    public void AddSession(ChatSession session)
    {
        _db.ChatSessions.Add(session);
    }

    // ── ChatMessage queries ──

    public async Task<EntityPage<ChatMessage>> GetSessionMessagesAsync(Guid sessionId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    // ── ChatMessage commands ──

    public void AddMessage(ChatMessage message)
    {
        _db.ChatMessages.Add(message);
    }

    // ── Unit of Work ──

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ──

    private static async Task<EntityPage<T>> PaginateAsync<T>(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new EntityPage<T>(items, totalCount, page, pageSize);
    }
}
