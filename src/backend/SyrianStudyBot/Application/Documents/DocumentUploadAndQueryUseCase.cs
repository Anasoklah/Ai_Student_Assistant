using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Mappers;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Documents.Configuration;
using SyrianStudyBot.Application.Documents.Validation;
using SyrianStudyBot.Application.Documents.Commands;
using Microsoft.Extensions.Options;

namespace SyrianStudyBot.Application.Documents;

/// <summary>
/// Handles the API-facing document actions: upload, status, and lists.
/// Long-running extraction is delegated to the background processing queue.
/// </summary>
public class DocumentUploadAndQueryUseCase : IDocumentUploadAndQueryUseCase
{
    private readonly IDocumentRepository _docRepo;
    private readonly IUserContextService _userContext;
    private readonly IDocumentValidator _documentValidator;
    private readonly IDocumentProcessingJobQueue _processingQueue;
    private readonly DocumentUploadOptions _uploadOptions;
    private readonly ILogger<DocumentUploadAndQueryUseCase> _logger;

    public DocumentUploadAndQueryUseCase(
        IDocumentRepository docRepo,
        IDocumentValidator documentValidator,
        IUserContextService userContext,
        IDocumentProcessingJobQueue processingQueue,
        IOptions<DocumentUploadOptions> uploadOptions,
        ILogger<DocumentUploadAndQueryUseCase> logger)
    {
        _docRepo = docRepo;
        _userContext = userContext;
        _documentValidator = documentValidator;
        _processingQueue = processingQueue;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var validationError = _documentValidator.ValidateUpload(command, _uploadOptions.MaxAdminFileSizeBytes);
        if (validationError is not null)
            throw new BadRequestException(validationError);

        var userId = _userContext.GetCurrentUserId();
        var ext = Path.GetExtension(command.FileName);


        // create a new document and save it in the database 
        var document = DocumentMappers
        .MapFromUploadDocumentCommandToEntity(command,userId);

        _docRepo.Add(document);
        await _docRepo.SaveChangesAsync(cancellationToken);

        // create a temp file in temp path to work with it temporary
        var tempDir = Path.Combine(Path.GetTempPath(), "ssb-uploads");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"{document.Id}{ext}");

        await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        {
            await command.FileContent.CopyToAsync(fileStream, cancellationToken);
        }

        // create a new Document Process Request to put it in the channel Queue
        var processingRequest = DocumentMappers
        .CreateDocumentProcessRequest(tempPath , document , command);

        // sent request to the Queue ... let background job complete the process 
        await _processingQueue.EnqueueAsync(processingRequest, cancellationToken);
        _logger.LogInformation("Queued background processing for document {Id}", document.Id);

        
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
