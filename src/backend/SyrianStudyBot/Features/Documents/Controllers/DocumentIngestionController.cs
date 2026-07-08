using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Features.Documents.UseCases;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Features.Common.Dtos;

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

    // Student upload endpoints are removed for this phase. Admin upload is the only supported ingestion flow.


    [HttpPost("{documentId:guid}/approval")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetApproval(Guid documentId, [FromQuery] bool approve, CancellationToken cancellationToken)
    {
        var document = await documentUseCase.SetApprovalAsync(documentId, approve, cancellationToken);
        return document is null ? NotFound(new { message = "Document not found" }) : Ok(document);
    }

    [HttpGet]
    [Authorize(Policy = "StudentOnly")]
    public async Task<ActionResult<PagedResponse<DocumentSummaryDto>>> GetApprovedDocuments(
        [FromQuery] Subject? subject,
        [FromQuery] GradeLevel? gradeLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await documentUseCase.GetApprovedDocumentsAsync(subject, gradeLevel, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponse<DocumentSummaryDto>>> GetDocumentsForAdmin(
        [FromQuery] bool? isApproved,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await documentUseCase.GetDocumentsForAdminAsync(isApproved, page, pageSize, cancellationToken);
        return Ok(response);
    }
}
