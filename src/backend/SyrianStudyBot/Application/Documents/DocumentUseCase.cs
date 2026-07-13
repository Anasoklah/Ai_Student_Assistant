using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Mappers;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Rag;
using SyrianStudyBot.Infrastructure.Documents;
using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace SyrianStudyBot.Application.Documents;

public class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _docRepo;
    private readonly IUserContextService _userContext;
    private readonly IDocumentIngestionValidator _documentValidator;
    private readonly IDocumentProcessingQueue _processingQueue;
    private readonly DocumentUploadOptions _uploadOptions;
    private readonly ILogger<DocumentUseCase> _logger;

    public DocumentUseCase(
        IDocumentRepository docRepo,
        IDocumentIngestionValidator documentValidator,
        IUserContextService userContext,
        IDocumentProcessingQueue processingQueue,
        IOptions<DocumentUploadOptions> uploadOptions,
        ILogger<DocumentUseCase> logger)
    {
        _docRepo = docRepo;
        _userContext = userContext;
        _documentValidator = documentValidator;
        _processingQueue = processingQueue;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = _documentValidator.ValidateFileUploadRequest(request, _uploadOptions.MaxAdminFileSizeBytes);
        if (validationError is not null)
            throw new BadRequestException(validationError);

        var userId = _userContext.GetCurrentUserId();
        var ext = Path.GetExtension(request.File.FileName);

        var document = new Document
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            DocumentType = DocumentType.OfficialBook,
            UploadedByUserId = userId,
            FileSizeBytes = request.File.Length,
            Status = DocumentStatus.Processing
        };

        _docRepo.Add(document);
        await _docRepo.SaveChangesAsync(cancellationToken);

        var tempDir = Path.Combine(Path.GetTempPath(), "ssb-uploads");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"{document.Id}{ext}");

        await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        {
            await request.File.CopyToAsync(fileStream, cancellationToken);
        }

        var job = new DocumentProcessingJob(
            DocumentId: document.Id,
            TempFilePath: tempPath,
            FileName: request.File.FileName,
            StartPage: request.StartPage,
            EndPage: request.EndPage,
            TocPage: request.TocPage,
            TocPageEnd: request.TocPageEnd,
            UploadedByUserId: userId
        );

        await _processingQueue.EnqueueAsync(job, cancellationToken);
        _logger.LogInformation("Enqueued processing job for document {Id}", document.Id);

        return DocumentMappers.MapToStudentDto(document);
    }

    public async Task<DocumentStatusDto> GetDocumentStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _docRepo.GetByIdAsync(id, cancellationToken);
        if (document is null)
            throw new NotFoundException($"Document with ID {id} not found.");

        return DocumentMappers.MapToStatusDto(document);
    }

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
}
