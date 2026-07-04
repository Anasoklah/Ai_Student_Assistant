using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Features.Rag.Dtos;

namespace SyrianStudyBot.Features.Rag.Controllers;

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

        var answer = await _ragPipeline.QueryAsync(request.Question, request.Mode, request.Subject, request.SectionFilter, request.ChapterFilter,cancellationToken);

        return Ok(new RagQueryResponseDto { Answer = answer });
    }
}
