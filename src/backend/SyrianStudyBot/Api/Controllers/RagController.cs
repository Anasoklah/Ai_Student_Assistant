using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.Rag.Dtos;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class RagController : ControllerBase
{
    private readonly IRagPipelineService _ragPipeline;

    public RagController(IRagPipelineService ragPipeline) => _ragPipeline = ragPipeline;

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] RagQueryRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("The question is required.");

        var answer = await _ragPipeline.QueryAsync(
            request.Question, request.Mode, request.Subject,
            request.DocumentId, request.ChapterId, request.SectionId,
            request.PageStart, request.PageEnd,
            cancellationToken);

        return Ok(new RagQueryResponseDto { Answer = answer });
    }
}
