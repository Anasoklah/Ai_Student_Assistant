using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Api.Contracts.Documents;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Documents.Commands;
using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Api.Controllers;

[ApiController]
[RequestFormLimits(MultipartBodyLengthLimit = 500L * 1024 * 1024)]
[RequestSizeLimit(500L * 1024 * 1024)]
[Route("api/documents")]
public class DocumentIngestionController(
    IDocumentUploadAndQueryUseCase documentUseCase) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(Policy = "AdminOnly")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAdminDocumentFile([FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var fileContent = request.File.OpenReadStream();
        var command = new UploadDocumentCommand
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            StartPage = request.StartPage,
            EndPage = request.EndPage,
            TocPage = request.TocPage,
            TocPageEnd = request.TocPageEnd,
            FileName = request.File.FileName,
            FileSizeBytes = request.File.Length,
            FileContent = fileContent
        };

        var document = await documentUseCase.UploadAsync(command, cancellationToken);
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
