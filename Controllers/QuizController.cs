using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.UseCases;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize(Policy = "StudentOnly")]
public class QuizController(
    IQuizUseCase quizUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateQuiz([FromBody] GenerateQuizRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var session = await quizUseCase.GenerateQuizAsync(userId, request, cancellationToken);
        return Ok(session);
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

        var response = await quizUseCase.GetHistoryAsync(userId, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{quizSessionId:guid}")]
    public async Task<IActionResult> GetQuiz(Guid quizSessionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var session = await quizUseCase.GetQuizAsync(userId, quizSessionId, cancellationToken);
        return session is null ? NotFound(new { message = "Quiz session not found" }) : Ok(session);
    }

    [HttpPost("{quizSessionId:guid}/submit")]
    public async Task<IActionResult> SubmitQuiz(Guid quizSessionId, [FromBody] SubmitQuizRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var result = await quizUseCase.SubmitQuizAsync(userId, quizSessionId, request, cancellationToken);
            return result is null ? NotFound(new { message = "Quiz session not found" }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
