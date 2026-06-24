using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Extensions;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize(Policy = "StudentOnly")]
public class QuizController(
    AppDbContext db,
    IRagPipelineService ragPipeline,
    IPagingService pagingService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateQuiz([FromBody] GenerateQuizRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var totalQuestions = Math.Clamp(request.TotalQuestions, 1, 20);
        var prompt = $"Generate {totalQuestions} exam questions for {request.Subject}.";
        var quizText = await ragPipeline.QueryAsync(prompt, ChatMode.Quiz, request.Subject, cancellationToken);

        var questions = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            content = quizText,
            generatedAt = DateTime.UtcNow
        }));

        var session = new QuizSession
        {
            UserId = userId,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            TotalQuestions = totalQuestions,
            Questions = questions
        };

        db.QuizSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(QuizMappers.MapSession(session));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuizSessionResponseDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.QuizSessions
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<QuizSessionResponseDto>
        {
            Items = items.Select(QuizMappers.MapSession).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("{quizSessionId:guid}")]
    public async Task<IActionResult> GetQuiz(Guid quizSessionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var session = await db.QuizSessions
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, cancellationToken);

        return session is null
            ? NotFound(new { message = "Quiz session not found" })
            : Ok(QuizMappers.MapSession(session));
    }

    [HttpPost("{quizSessionId:guid}/submit")]
    public async Task<IActionResult> SubmitQuiz(Guid quizSessionId, [FromBody] SubmitQuizRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        if (request.MaxScore <= 0 || request.Score < 0 || request.Score > request.MaxScore)
            return BadRequest(new { message = "Invalid score" });

        var session = await db.QuizSessions
            .Include(q => q.Result)
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, cancellationToken);

        if (session is null)
            return NotFound(new { message = "Quiz session not found" });

        if (session.IsCompleted)
            return Conflict(new { message = "Quiz is already completed" });

        session.Answers = JsonDocument.Parse(request.Answers.GetRawText());
        session.Score = request.Score;
        session.MaxScore = request.MaxScore;
        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow;

        var result = new QuizResult
        {
            UserId = userId,
            QuizSessionId = session.Id,
            Subject = session.Subject!.Value,
            Score = request.Score,
            MaxScore = request.MaxScore,
            CompletedAt = session.CompletedAt.Value
        };

        db.QuizResults.Add(result);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(QuizMappers.MapResult(result));
    }
}
