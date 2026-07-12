using SyrianStudyBot.Features.Documents.Mappers;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Documents;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Common;
using Microsoft.Extensions.Options;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;
using SyrianStudyBot.Features.contracts.repositories;
using SyrianStudyBot.Features.contracts.services;

namespace SyrianStudyBot.Features.Documents.UseCases;

/// <summary>
/// Orchestrates document upload and ingestion: extracts pages from PDF,
/// optionally extracts book structure (TOC), and delegates to
/// DocumentIngestionService for chunking and embedding.
/// Relies on IDocumentRepository for all database operations.
/// </summary>
public class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _docRepo;
    private readonly IUserContextService _userContext;
    private readonly IDocumentIngestionService _ingestion;
    private readonly IDocumentIngestionValidator _documentValidator;
    private readonly IExtractionService _ExtractionService;
    private readonly DocumentUploadOptions _uploadOptions;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    public DocumentUseCase(
        IDocumentRepository docRepo,
        IDocumentIngestionService ingestion,
        IDocumentIngestionValidator documentValidator,
        IExtractionService ExtractionService,
        IUserContextService userContext,
        IOptions<DocumentUploadOptions> uploadOptions)
    {
        _docRepo = docRepo;
        _userContext = userContext;
        _ingestion = ingestion;
        _documentValidator = documentValidator;
        _ExtractionService = ExtractionService;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<DocumentDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
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

        // Extract book structure if TocPage is provided
        BookStructureDto? structure = null;
        if (request.TocPage.HasValue && ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var structureResult = await _ExtractionService.ExtractStructureAsync(
                    request.File.OpenReadStream(),
                    request.TocPage.Value,
                    cancellationToken);

                if (structureResult is not null)
                {
                    structure = DocumentMappers.ToBookStructureDto(structureResult);
                }
            }
            catch (Exception)
            {
                // Graceful degradation: structure extraction failure should not block ingestion
            }
        }

        var ingestionRequest = DocumentMappers.ToIngestionCommand(request, pages, _userContext.GetCurrentUserId(), structure);
        ValidateReadablePages(ingestionRequest);

        var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);
        return DocumentMappers.MapToStudentDto(document);
    }

    // Student endpoint: GET /api/documents
    public async Task<PagedResponse<DocumentDto>> GetMyDocumentsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId();
        var entityPage = await _docRepo.GetUserDocumentsAsync(userId, page, pageSize, cancellationToken);

        return new PagedResponse<DocumentDto>(
            entityPage.Items.Select(d => DocumentMappers.MapToStudentDto(d)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    // Admin endpoint: GET /api/admin/documents
    public async Task<PagedResponse<AdminDocumentDto>> GetAllDocumentsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var entityPage = await _docRepo.GetAllDocumentsAsync(page, pageSize, cancellationToken);

        return new PagedResponse<AdminDocumentDto>(
            entityPage.Items.Select(d => DocumentMappers.MapToAdminDto(d)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    #region Helpers
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

    private void ValidateReadablePages(DocumentIngestionCommand request)
    {
        var validationError = _documentValidator.ValidateIngestionRequest(request);
        if (validationError is not null)
            throw new BadRequestException(validationError);
    }

    #endregion
}
