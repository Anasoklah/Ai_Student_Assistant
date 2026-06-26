using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.UseCases;
using SyrianStudyBot.Common.Extensions;
using SyrianStudyBot.Common.Validators;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentIngestionController(
    IDocumentUseCase documentUseCase,
    UserManager<ApplicationUser> userManager,
    IDocumentIngestionValidator documentValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> IngestDocument([FromBody] DocumentIngestionRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var document = await documentUseCase.IngestDocumentAsync(request, cancellationToken);
        return Ok(document);
    }

    [HttpPost("student-upload")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> UploadStudentDocument([FromBody] DocumentIngestionRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var document = await documentUseCase.UploadStudentDocumentAsync(request, user, cancellationToken);
            return Ok(document);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message == "Upload forbidden"
                ? Forbid()
                : StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
    }

    [HttpPost("{documentId:guid}/approval")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetApproval(Guid documentId, [FromQuery] bool approve, CancellationToken cancellationToken)
    {
        var document = await documentUseCase.SetApprovalAsync(documentId, approve, cancellationToken);
        return document is null ? NotFound(new { message = "Document not found" }) : Ok(document);
    }

    [HttpGet]
    [Authorize(Policy = "StudentOnly")]
    public async Task<ActionResult<PagedResponse<DocumentIngestionResultDto>>> GetApprovedDocuments(
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
    public async Task<ActionResult<PagedResponse<DocumentIngestionResultDto>>> GetDocumentsForAdmin(
        [FromQuery] bool? isApproved,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await documentUseCase.GetDocumentsForAdminAsync(isApproved, page, pageSize, cancellationToken);
        return Ok(response);
    }
}
