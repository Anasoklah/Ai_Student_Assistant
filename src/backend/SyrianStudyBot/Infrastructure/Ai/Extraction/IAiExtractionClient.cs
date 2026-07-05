using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Infrastructure.Ai.Extraction;

public interface IAiExtractionClient
{
    Task<JobAcceptedResponse> SubmitExtractionJobAsync(
        byte[] pdfBytes,
        string bookId,
        int pageStart = 1,
        int? pageEnd = null,
        CancellationToken cancellationToken = default);

    Task<JobStatusResponse> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

    Task<JobResultResponse> GetJobResultAsync(string jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesFromJobAsync(
        string jobId,
        Func<Task>? beforeVisionExtraction = null,
        CancellationToken cancellationToken = default);
}
