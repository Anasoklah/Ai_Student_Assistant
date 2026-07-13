using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Features.Documents.UseCases;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Features.Documents.Controllers;

[ApiController]
[RequestFormLimits(MultipartBodyLengthLimit = 200L * 1024 * 1024)]
[RequestSizeLimit(200L * 1024 * 1024)]
[Route("api/documents")]
public class DocumentIngestionController(
    IDocumentUseCase documentUseCase) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(Policy = "AdminOnly")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAdminDocumentFile([FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        var document = await documentUseCase.IngestUploadedDocumentAsync(request, cancellationToken);
        return Ok(document);
    }

    [HttpGet("{id}/status")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<ActionResult<DocumentStatusDto>> GetDocumentStatus(
        Guid id, CancellationToken cancellationToken)
    {
        var status = await documentUseCase.GetDocumentStatusAsync(id, cancellationToken);
        return Ok(status);
    }

    [HttpGet]
    [Authorize(Policy = "StudentOnly")]
    public async Task<ActionResult<PagedResponse<DocumentDto>>> GetApprovedDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await documentUseCase.GetMyDocumentsAsync( page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponse<AdminDocumentDto>>> GetDocumentsForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await documentUseCase.GetAllDocumentsAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }
}
