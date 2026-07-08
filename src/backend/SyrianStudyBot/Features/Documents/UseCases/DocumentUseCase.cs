using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Features.Documents.Mappers;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Documents;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Features.Common.Dtos;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Infrastructure.Common;
using Microsoft.Extensions.Options;
using SyrianStudyBot.Infrastructure.Identity;

namespace SyrianStudyBot.Features.Documents.UseCases;

public class DocumentUseCase : IDocumentUseCase
{
    private readonly AppDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly IDocumentIngestionService _ingestion;
    private readonly IPagingService _pagingService;
    private readonly IDocumentIngestionValidator _documentValidator;
    private readonly IExtractionService _ExtractionService;
    private readonly DocumentUploadOptions _uploadOptions;

    public DocumentUseCase(
        AppDbContext db,
        IDocumentIngestionService ingestion,
        IPagingService pagingService,
        IDocumentIngestionValidator documentValidator,
        IExtractionService ExtractionService,
        IUserContextService userContext,
        IOptions<DocumentUploadOptions> uploadOptions)
    {
        _db = db;
        _userContext = userContext;
        _ingestion = ingestion;
        _pagingService = pagingService;
        _documentValidator = documentValidator;
        _ExtractionService = ExtractionService;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<DocumentSummaryDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = _documentValidator.ValidateFileUploadRequest(request, _uploadOptions.MaxAdminFileSizeBytes);
        if (validationError is not null)
            throw new BadRequestException(validationError);

        var fileBytes = await GetFileBytesAsync(request.File, cancellationToken);
        var pages = await _ExtractionService.ExtractPagesAsync(fileBytes,
        request.StartPage ,
         request.EndPage,
          cancellationToken);
        var ingestionRequest = DocumentMappers.ToIngestionCommand(request, pages , _userContext.GetCurrentUserId());
        ValidateReadablePages(ingestionRequest);

        var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);
        return DocumentMappers.MapToDto(document);
    }

    // Student upload methods are intentionally removed for this phase.
    // We focus on admin upload ingestion only and will restore student upload support later.

    public async Task<DocumentSummaryDto> SetApprovalAsync(Guid documentId, bool approve, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
            return null!;

        document.IsApproved = approve;
        await _db.SaveChangesAsync(cancellationToken);
        return DocumentMappers.MapToDto(document);
    }

    public async Task<PagedResponse<DocumentSummaryDto>> GetApprovedDocumentsAsync(Subject? subject, GradeLevel? gradeLevel, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var query = _db.Documents.Where(d => d.IsApproved);
        if (subject.HasValue)
            query = query.Where(d => d.Subject == subject.Value);
        if (gradeLevel.HasValue)
            query = query.Where(d => d.GradeLevel == gradeLevel.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DocumentMappers.MapToDto(d))
            .ToListAsync(cancellationToken);

        return new PagedResponse<DocumentSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResponse<DocumentSummaryDto>> GetDocumentsForAdminAsync(bool? isApproved, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var query = _db.Documents.AsQueryable();
        if (isApproved.HasValue)
            query = query.Where(d => d.IsApproved == isApproved.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DocumentMappers.MapToDto(d))
            .ToListAsync(cancellationToken);

        return new PagedResponse<DocumentSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static async Task<byte[]> GetFileBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private void ValidateReadablePages(DocumentIngestionCommand request)
    {
        var validationError = _documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            throw new BadRequestException(validationError);
    }
}
