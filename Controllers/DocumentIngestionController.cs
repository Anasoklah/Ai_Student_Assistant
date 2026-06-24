using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Extensions;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Common.Validators;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentIngestionController(
    IDocumentIngestionService ingestion,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IPagingService pagingService,
    IUsageTrackingService usageTrackingService,
    IDocumentIngestionValidator documentValidator,
    IDocumentRequestService documentRequestService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> IngestDocument([FromBody] DocumentIngestionRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var adminRequest = documentRequestService.CreateAdminRequest(request);
        var document = await ingestion.IngestAsync(adminRequest, cancellationToken);

        return Ok(DocumentMappers.MapDocument(document));
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

        usageTrackingService.ResetUploadCounterIfNeeded(user);
        if (!SubscriptionRules.CanUpload(user.SubscriptionTier))
            return Forbid();

        var monthlyLimit = SubscriptionRules.GetMonthlyUploadLimit(user.SubscriptionTier);
        if (user.UploadsThisMonth >= monthlyLimit)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Monthly upload limit reached" });

        var studentRequest = documentRequestService.CreateStudentRequest(request, userId);
        var document = await ingestion.IngestAsync(studentRequest, cancellationToken);

        user.UploadsThisMonth++;
        await usageTrackingService.UpsertUploadUsageAsync(user.Id, cancellationToken);
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(DocumentMappers.MapDocument(document));
    }

    [HttpPost("{documentId:guid}/approval")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetApproval(Guid documentId, [FromQuery] bool approve, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
            return NotFound(new { message = "Document not found" });

        document.IsApproved = approve;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(DocumentMappers.MapDocument(document));
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
        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.Documents.Where(d => d.IsApproved);
        if (subject.HasValue)
            query = query.Where(d => d.Subject == subject.Value);
        if (gradeLevel.HasValue)
            query = query.Where(d => d.GradeLevel == gradeLevel.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DocumentMappers.MapDocument(d))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<DocumentIngestionResultDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponse<DocumentIngestionResultDto>>> GetDocumentsForAdmin(
        [FromQuery] bool? isApproved,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.Documents.AsQueryable();
        if (isApproved.HasValue)
            query = query.Where(d => d.IsApproved == isApproved.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DocumentMappers.MapDocument(d))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<DocumentIngestionResultDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }
}
