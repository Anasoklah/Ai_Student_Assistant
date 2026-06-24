using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly IRagPipelineService _ragPipeline;

    public RagController(IRagPipelineService ragPipeline) => _ragPipeline = ragPipeline;

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] RagQueryRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("The question is required.");

        var answer = await _ragPipeline.QueryAsync(request.Question, request.Mode, request.Subject, cancellationToken);

        return Ok(new RagQueryResponseDto { Answer = answer });
    }
}
