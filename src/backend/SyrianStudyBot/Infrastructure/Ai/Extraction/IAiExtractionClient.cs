using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Infrastructure.Ai.Extraction;

public interface IAiExtractionClient
{
    Task<JobAcceptedResponse> SubmitExtractionJobAsync(
        Stream pdfStream,
        string bookId,
        int? pageStart = null ,
        int? pageEnd = null,
        CancellationToken cancellationToken = default);

    Task<JobStatusResponse> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

    Task<JobResultResponse> GetJobResultAsync(string jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesFromJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    Task<ImageExtractionResponse> ExtractImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<StructureExtractionResponse> ExtractBookStructureAsync(
        Stream pdfStream,
        int tocPage,
        int? tocPageEnd,
        CancellationToken cancellationToken = default);
}
