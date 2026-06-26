using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Common.Validators;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Application.UseCases;

public interface IDocumentUseCase
{
    Task<DocumentIngestionResultDto> IngestDocumentAsync(DocumentIngestionRequestDto request, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> IngestUploadedDocumentAsync(DocumentFileUploadRequestDto request, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> UploadStudentDocumentAsync(DocumentIngestionRequestDto request, ApplicationUser user, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> UploadStudentDocumentFileAsync(DocumentFileUploadRequestDto request, ApplicationUser user, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> SetApprovalAsync(Guid documentId, bool approve, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentIngestionResultDto>> GetApprovedDocumentsAsync(Subject? subject, GradeLevel? gradeLevel, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentIngestionResultDto>> GetDocumentsForAdminAsync(bool? isApproved, int page, int pageSize, CancellationToken cancellationToken = default);
}

public class DocumentUseCase : IDocumentUseCase
{
    private readonly AppDbContext _db;
    private readonly IDocumentIngestionService _ingestion;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IDocumentRequestService _documentRequestService;
    private readonly IPagingService _pagingService;
    private readonly IDocumentIngestionValidator _documentValidator;
    private readonly IDocumentFileExtractionService _fileExtractionService;
    private readonly IDocumentFileStorageService _fileStorageService;
    private readonly DocumentUploadOptions _uploadOptions;

    public DocumentUseCase(
        AppDbContext db,
        IDocumentIngestionService ingestion,
        IUsageTrackingService usageTrackingService,
        IDocumentRequestService documentRequestService,
        IPagingService pagingService,
        IDocumentIngestionValidator documentValidator,
        IDocumentFileExtractionService fileExtractionService,
        IDocumentFileStorageService fileStorageService,
        IOptions<DocumentUploadOptions> uploadOptions)
    {
        _db = db;
        _ingestion = ingestion;
        _usageTrackingService = usageTrackingService;
        _documentRequestService = documentRequestService;
        _pagingService = pagingService;
        _documentValidator = documentValidator;
        _fileExtractionService = fileExtractionService;
        _fileStorageService = fileStorageService;
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
            throw new InvalidOperationException(validationError);

        StoredDocumentFile? storedFile = null;
        try
        {
            storedFile = await _fileStorageService.SaveAsync(request.File, DocumentType.OfficialBook, userId: null, cancellationToken);
            var pages = await _fileExtractionService.ExtractPagesAsync(request.File, request.ForceVision, cancellationToken);
            var ingestionRequest = _documentRequestService.CreateAdminFileRequest(request, pages, storedFile);
            ValidateReadablePages(ingestionRequest);

            var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);
            return DocumentMappers.MapDocument(document);
        }
        catch
        {
            _fileStorageService.DeleteIfExists(storedFile?.FilePath);
            throw;
        }
    }

    public async Task<DocumentIngestionResultDto> UploadStudentDocumentAsync(DocumentIngestionRequestDto request, ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _usageTrackingService.ResetUploadCounterIfNeeded(user);
        if (!SubscriptionRules.CanUpload(user.SubscriptionTier))
            throw new InvalidOperationException("Upload forbidden");

        var monthlyLimit = SubscriptionRules.GetMonthlyUploadLimit(user.SubscriptionTier);
        if (user.UploadsThisMonth >= monthlyLimit)
            throw new InvalidOperationException("Monthly upload limit reached");

        var studentRequest = _documentRequestService.CreateStudentRequest(request, user.Id);
        var document = await _ingestion.IngestAsync(studentRequest, cancellationToken);

        user.UploadsThisMonth++;
        await _usageTrackingService.UpsertUploadUsageAsync(user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return DocumentMappers.MapDocument(document);
    }

    public async Task<DocumentIngestionResultDto> UploadStudentDocumentFileAsync(DocumentFileUploadRequestDto request, ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _usageTrackingService.ResetUploadCounterIfNeeded(user);
        if (!SubscriptionRules.CanUpload(user.SubscriptionTier))
            throw new InvalidOperationException("Upload forbidden");

        var monthlyLimit = SubscriptionRules.GetMonthlyUploadLimit(user.SubscriptionTier);
        if (user.UploadsThisMonth >= monthlyLimit)
            throw new InvalidOperationException("Monthly upload limit reached");

        var fileSizeLimit = SubscriptionRules.GetMaxUploadFileSizeBytes(user.SubscriptionTier);
        var validationError = _documentValidator.ValidateFileUploadRequest(request, fileSizeLimit);
        if (validationError is not null)
            throw new InvalidOperationException(validationError);

        StoredDocumentFile? storedFile = null;
        try
        {
            storedFile = await _fileStorageService.SaveAsync(request.File, DocumentType.StudentUpload, user.Id, cancellationToken);
            var pages = await _fileExtractionService.ExtractPagesAsync(request.File, request.ForceVision, cancellationToken);
            var ingestionRequest = _documentRequestService.CreateStudentFileRequest(request, user.Id, pages, storedFile);
            ValidateReadablePages(ingestionRequest);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var document = await _ingestion.IngestAsync(ingestionRequest, cancellationToken);

            user.UploadsThisMonth++;
            await _usageTrackingService.UpsertUploadUsageAsync(user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return DocumentMappers.MapDocument(document);
        }
        catch
        {
            _fileStorageService.DeleteIfExists(storedFile?.FilePath);
            throw;
        }
    }

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
            throw new InvalidOperationException(validationError);
    }
}
