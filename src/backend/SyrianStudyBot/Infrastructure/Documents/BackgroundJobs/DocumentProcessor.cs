using Microsoft.Extensions.Logging;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Mappers;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Rag;
using SyrianStudyBot.Infrastructure.Documents.Validation;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

public class DocumentProcessor : IDocumentProcessor
{
    private readonly IDocumentRepository _docRepo;
    private readonly IExtractionService _extractionService;
    private readonly IDocumentIngestionService _ingestion;
    private readonly IDocumentIngestionValidator _validator;
    private readonly ILogger<DocumentProcessor> _logger;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    public DocumentProcessor(
        IDocumentRepository docRepo,
        IExtractionService extractionService,
        IDocumentIngestionService ingestion,
        IDocumentIngestionValidator validator,
        ILogger<DocumentProcessor> logger)
    {
        _docRepo = docRepo;
        _extractionService = extractionService;
        _ingestion = ingestion;
        _validator = validator;
        _logger = logger;
    }

    public async Task ProcessAsync(DocumentProcessingJob job, CancellationToken ct = default)
    {
        var document = await _docRepo.GetByIdAsync(job.DocumentId, ct);
        if (document is null)
        {
            _logger.LogWarning("Document {Id} not found for processing", job.DocumentId);
            return;
        }

        try
        {
            await using var fileStream = new FileStream(job.TempFilePath, FileMode.Open, FileAccess.Read);
            var ext = Path.GetExtension(job.FileName);

            IReadOnlyList<ExtractedPageDto> pages;
            if (ImageExtensions.Contains(ext))
            {
                pages = await ExtractImageAsync(fileStream, job.FileName, ct);
            }
            else
            {
                pages = await _extractionService.ExtractPagesAsync(
                    fileStream, job.StartPage, job.EndPage, ct);
            }

            BookStructureDto? structure = null;
            if (job.TocPage.HasValue && ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                structure = await ExtractStructureAsync(fileStream, job.TocPage.Value, job.TocPageEnd, ct);
            }

            var validationError = _validator.ValidateIngestionRequest(new DocumentIngestionCommand
            {
                Title = document.Title,
                SourceName = document.SourceName,
                Pages = pages
            });
            if (validationError is not null)
            {
                document.Status = DocumentStatus.Failed;
                document.StatusMessage = validationError;
                document.ProcessedAt = DateTime.UtcNow;
                await _docRepo.SaveChangesAsync(ct);
                _logger.LogWarning("Document {Id} failed validation: {Error}", job.DocumentId, validationError);
                return;
            }

            await _ingestion.AttachExtractedContentAsync(document, pages, structure, ct);

            document.Status = DocumentStatus.Ready;
            document.StatusMessage = null;
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Document {Id} processed successfully", job.DocumentId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Document {Id} processing was cancelled", job.DocumentId);
            document.Status = DocumentStatus.Failed;
            document.StatusMessage = "Processing was cancelled";
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {Id}", job.DocumentId);
            document.Status = DocumentStatus.Failed;
            document.StatusMessage = ex.Message;
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            TryDeleteTempFile(job.TempFilePath);
        }
    }

    private async Task<IReadOnlyList<ExtractedPageDto>> ExtractImageAsync(
        Stream imageStream, string fileName, CancellationToken ct)
    {
        var result = await _extractionService.ExtractImageAsync(imageStream, fileName, ct);
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

    private async Task<BookStructureDto?> ExtractStructureAsync(
        Stream pdfStream, int tocPage, int? tocPageEnd, CancellationToken ct)
    {
        try
        {
            pdfStream.Position = 0;
            var result = await _extractionService.ExtractStructureAsync(pdfStream, tocPage, tocPageEnd, ct);
            return result is not null ? DocumentMappers.ToBookStructureDto(result) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Structure extraction failed for document, continuing without it");
            return null;
        }
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            // Best-effort cleanup
        }
    }
}
