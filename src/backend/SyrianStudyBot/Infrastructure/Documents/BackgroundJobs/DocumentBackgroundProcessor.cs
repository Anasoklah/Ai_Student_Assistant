using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Documents.Validation;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

/// <summary>
/// Infrastructure worker that opens the temporary file and coordinates the
/// application extraction and ingestion ports.
/// </summary>
public class DocumentBackgroundProcessor
{
    private readonly IDocumentRepository _docRepo;
    private readonly IDocumentContentExtractor _extractor;
    private readonly IDocumentContentIngestionService _contentIngestionService;
    private readonly IDocumentValidator _validator;
    private readonly ILogger<DocumentBackgroundProcessor> _logger;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    public DocumentBackgroundProcessor(
        IDocumentRepository docRepo,
        IDocumentContentExtractor extractor,
        IDocumentContentIngestionService contentIngestionService,
        IDocumentValidator validator,
        ILogger<DocumentBackgroundProcessor> logger)
    {
        _docRepo = docRepo;
        _extractor = extractor;
        _contentIngestionService = contentIngestionService;
        _validator = validator;
        _logger = logger;
    }

    public async Task ProcessAsync(DocumentProcessingRequest request, CancellationToken ct = default)
    {
        var document = await _docRepo.GetByIdAsync(request.DocumentId, ct);
        if (document is null)
        {
            _logger.LogWarning("Document {Id} not found for processing", request.DocumentId);
            return;
        }

        try
        {
            await using var fileStream = new FileStream(request.TempFilePath, FileMode.Open, FileAccess.Read);
            var ext = Path.GetExtension(request.FileName);

            IReadOnlyList<ExtractedPageDto> pages;
            if (ImageExtensions.Contains(ext))
            {
                pages = await ExtractImageAsync(fileStream, request.FileName, ct);
            }
            else
            {
                pages = await _extractor.ExtractPdfAsync(
                    fileStream, request.StartPage, request.EndPage, ct);
            }

            BookStructureDto? structure = null;
            if (request.TocPage.HasValue && ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Page extraction above consumes/disposes fileStream (multipart upload),
                // so open a fresh stream for structure extraction instead of reusing it.
                await using var structureStream = new FileStream(request.TempFilePath, FileMode.Open, FileAccess.Read);
                structure = await ExtractStructureAsync(structureStream, request.TocPage.Value, request.TocPageEnd, ct);
            }

            var validationError = _validator.ValidateExtractedContent(pages);
            if (validationError is not null)
            {
                document.Status = DocumentStatus.Failed;
                document.StatusMessage = validationError;
                document.ProcessedAt = DateTime.UtcNow;
                await _docRepo.SaveChangesAsync(ct);
                _logger.LogWarning("Document {Id} failed validation: {Error}", request.DocumentId, validationError);
                return;
            }

            await _contentIngestionService.AttachExtractedContentAsync(document, pages, structure, ct);

            document.Status = DocumentStatus.Ready;
            document.StatusMessage = null;
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Document {Id} processed successfully", request.DocumentId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Document {Id} processing was cancelled", request.DocumentId);
            document.Status = DocumentStatus.Failed;
            document.StatusMessage = "Processing was cancelled";
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {Id}", request.DocumentId);
            document.Status = DocumentStatus.Failed;
            document.StatusMessage = ex.Message;
            document.ProcessedAt = DateTime.UtcNow;
            await _docRepo.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            TryDeleteTempFile(request.TempFilePath);
        }
    }

    private async Task<IReadOnlyList<ExtractedPageDto>> ExtractImageAsync(
        Stream imageStream, string fileName, CancellationToken ct)
    {
        var result = await _extractor.ExtractImageAsync(imageStream, fileName, ct);
        return new List<ExtractedPageDto>
        {
            new()
            {
                PageNumber = 1,
                Text = string.Join("\n", result.Concepts.Select(c => $"{c.Title}\n{c.Content}")),
                NeedsReview = result.NeedsReview,
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
            return await _extractor.ExtractBookStructureAsync(pdfStream, tocPage, tocPageEnd, ct);
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
