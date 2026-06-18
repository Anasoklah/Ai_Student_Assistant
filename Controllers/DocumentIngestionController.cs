using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentIngestionController : ControllerBase
{
    private readonly IDocumentIngestionService _ingestion;

    public DocumentIngestionController(IDocumentIngestionService ingestion) => _ingestion = ingestion;

    [HttpPost]
    public async Task<IActionResult> IngestDocument([FromBody] DocumentIngestionRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest("Title and Subject are required.");

        var document = await _ingestion.IngestAsync(request, cancellationToken);

        return Ok(new DocumentIngestionResultDto
        {
            Id = document.Id,
            Title = document.Title,
            Subject = document.Subject,
            SourceName = document.SourceName,
            Edition = document.Edition,
            Language = document.Language,
            ChunkCount = document.Chunks.Count
        });
    }
}
