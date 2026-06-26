using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    Task<DocumentIngestionResultDto> UploadStudentDocumentAsync(DocumentIngestionRequestDto request, ApplicationUser user, CancellationToken cancellationToken = default);
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

    public DocumentUseCase(
        AppDbContext db,
        IDocumentIngestionService ingestion,
        IUsageTrackingService usageTrackingService,
        IDocumentRequestService documentRequestService,
        IPagingService pagingService)
    {
        _db = db;
        _ingestion = ingestion;
        _usageTrackingService = usageTrackingService;
        _documentRequestService = documentRequestService;
        _pagingService = pagingService;
    }

    public async Task<DocumentIngestionResultDto> IngestDocumentAsync(DocumentIngestionRequestDto request, CancellationToken cancellationToken = default)
    {
        var adminRequest = _documentRequestService.CreateAdminRequest(request);
        var document = await _ingestion.IngestAsync(adminRequest, cancellationToken);
        return DocumentMappers.MapDocument(document);
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
}
