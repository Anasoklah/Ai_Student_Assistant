using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Common;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles all database operations for QuizSession and QuizResult entities.
/// All quiz-related queries and commands go through this repository.
/// </summary>
public class QuizRepository : IQuizRepository
{
    private readonly AppDbContext _db;

    public QuizRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── QuizSession queries ──

    public async Task<EntityPage<QuizSession>> GetUserQuizzesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.QuizSessions
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<QuizSession?> GetQuizByIdAsync(Guid quizSessionId, Guid userId, CancellationToken ct = default)
    {
        return await _db.QuizSessions
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, ct);
    }

    public async Task<QuizSession?> GetQuizWithResultAsync(Guid quizSessionId, Guid userId, CancellationToken ct = default)
    {
        return await _db.QuizSessions
            .Include(q => q.Result)
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, ct);
    }

    // ── QuizSession commands ──

    public void AddQuiz(QuizSession session)
    {
        _db.QuizSessions.Add(session);
    }

    // ── QuizResult commands ──

    public void AddResult(QuizResult result)
    {
        _db.QuizResults.Add(result);
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
