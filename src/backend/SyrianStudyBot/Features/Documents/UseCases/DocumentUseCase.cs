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

namespace SyrianStudyBot.Features.Documents.UseCases;

public class DocumentUseCase : IDocumentUseCase
{
    private readonly AppDbContext _db;
    private readonly IDocumentIngestionService _ingestion;
    private readonly IDocumentRequestService _documentRequestService;
    private readonly IPagingService _pagingService;
    private readonly IDocumentIngestionValidator _documentValidator;
    private readonly IDocumentFileExtractionService _fileExtractionService;
    private readonly DocumentUploadOptions _uploadOptions;

    public DocumentUseCase(
        AppDbContext db,
        IDocumentIngestionService ingestion,
        IDocumentRequestService documentRequestService,
        IPagingService pagingService,
        IDocumentIngestionValidator documentValidator,
        IDocumentFileExtractionService fileExtractionService,
        IOptions<DocumentUploadOptions> uploadOptions)
    {
        _db = db;
        _ingestion = ingestion;
        _documentRequestService = documentRequestService;
        _pagingService = pagingService;
        _documentValidator = documentValidator;
        _fileExtractionService = fileExtractionService;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<DocumentIngestionResultDto> IngestDocumentAsync(DocumentIngestionRequestDto request, CancellationToken cancellationToken = default)
    {
        var adminRequest = _documentRequestService.CreateAdminRequest(request);
        var document = await _ingestion.IngestAsync(adminRequest, cancellationToken);
        return DocumentMappers.MapDocument(document);
    }

    public async Task<DocumentIngestionResultDto> IngestUploadedDocumentAsync(DocumentFileUploadRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationError = _documentValidator.ValidateFileUploadRequest(request, _uploadOptions.MaxAdminFileSizeBytes);
        if (validationError is not null)
            throw new BadRequestException(validationError);

        var pages = await _fileExtractionService.ExtractPagesAsync(request.File, request.ForceVision, cancellationToken);
        var ingestionRequest = _documentRequestService.CreateAdminFileRequest(request, pages);
        ValidateReadablePages(ingestionRequest);

        var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);
        return DocumentMappers.MapDocument(document);
    }

    // Student upload methods are intentionally removed for this phase.
    // We focus on admin upload ingestion only and will restore student upload support later.

    public async Task<DocumentIngestionResultDto> SetApprovalAsync(Guid documentId, bool approve, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
            return null!;

        document.IsApproved = approve;
        await _db.SaveChangesAsync(cancellationToken);
        return DocumentMappers.MapDocument(document);
    }

    public async Task<PagedResponse<DocumentIngestionResultDto>> GetApprovedDocumentsAsync(Subject? subject, GradeLevel? gradeLevel, int page, int pageSize, CancellationToken cancellationToken = default)
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
            .Select(d => DocumentMappers.MapDocument(d))
            .ToListAsync(cancellationToken);

        return new PagedResponse<DocumentIngestionResultDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResponse<DocumentIngestionResultDto>> GetDocumentsForAdminAsync(bool? isApproved, int page, int pageSize, CancellationToken cancellationToken = default)
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
            .Select(d => DocumentMappers.MapDocument(d))
            .ToListAsync(cancellationToken);

        return new PagedResponse<DocumentIngestionResultDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private void ValidateReadablePages(DocumentIngestionRequestDto request)
    {
        var validationError = _documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            throw new BadRequestException(validationError);
    }
}
