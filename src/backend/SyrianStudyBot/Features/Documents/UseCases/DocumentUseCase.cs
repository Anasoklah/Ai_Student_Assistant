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
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

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

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

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

        var ext = Path.GetExtension(request.File.FileName);

        IReadOnlyList<ExtractedPageDto> pages;

        if (ImageExtensions.Contains(ext))
        {
            // Route single images to the vision extraction endpoint
            var imageResult = await _ExtractionService.ExtractImageAsync(
                request.File.OpenReadStream(),
                request.File.FileName,
                cancellationToken);

            if (!imageResult.Success)
                throw new BadRequestException($"Image extraction failed: {imageResult.ErrorMessage}");

            pages = ConvertImageResultToPages(imageResult);
        }
        else
        {
            // Route PDFs (and TXT/MD) to the standard extraction pipeline
            pages = await _ExtractionService.ExtractPagesAsync(
                request.File.OpenReadStream(),
                request.StartPage,
                request.EndPage,
                cancellationToken);
        }

        var ingestionRequest = DocumentMappers.ToIngestionCommand(request, pages, _userContext.GetCurrentUserId());
        ValidateReadablePages(ingestionRequest);

        var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);
        return DocumentMappers.MapToDto(document);
    }

    private static IReadOnlyList<ExtractedPageDto> ConvertImageResultToPages(ImageExtractionResponse result)
    {
        return new List<ExtractedPageDto>
        {
            new()
            {
                PageNumber = 1,
                Text = string.Join("\n", result.Concepts.Select(c => $"{c.Title}\n{c.Content}")),
                Concepts = result.Concepts.Select(c => new ExtractedConceptDto
                {
                    Title = c.Title,
                    Content = c.Content,
                    Keywords = c.Keywords
                }).ToList()
            }
        };
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

    private void ValidateReadablePages(DocumentIngestionCommand request)
    {
        var validationError = _documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            throw new BadRequestException(validationError);
    }
}
