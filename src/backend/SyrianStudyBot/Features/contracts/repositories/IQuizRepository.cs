using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Infrastructure.Persistence.Repositories;

namespace SyrianStudyBot.Features.contracts.repositories;

/// <summary>
/// Repository for all Quiz-related database operations.
/// Covers: QuizSession, QuizResult.
///
/// Replaces direct AppDbContext usage in QuizUseCase.
/// </summary>
public interface IQuizRepository
{
    // ── QuizSession queries ──

    /// <summary>
    /// Returns a paginated list of quiz sessions for a user.
    /// Ordered by most recent first.
    /// </summary>
    Task<EntityPage<QuizSession>> GetUserQuizzesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a single quiz session by ID if it belongs to the user.
    /// Returns null if not found.
    /// </summary>
    Task<QuizSession?> GetQuizByIdAsync(Guid quizSessionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single quiz session by ID, including its QuizResult.
    /// Returns null if not found.
    /// </summary>
    Task<QuizSession?> GetQuizWithResultAsync(Guid quizSessionId, Guid userId, CancellationToken ct = default);

    // ── QuizSession commands ──

    /// <summary>
    /// Stages a new QuizSession for insertion.
    /// </summary>
    void AddQuiz(QuizSession session);

    // ── QuizResult commands ──

    /// <summary>
    /// Stages a new QuizResult for insertion.
    /// </summary>
    void AddResult(QuizResult result);

    // ── Unit of Work ──

    /// <summary>
    /// Persists all tracked entity changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
