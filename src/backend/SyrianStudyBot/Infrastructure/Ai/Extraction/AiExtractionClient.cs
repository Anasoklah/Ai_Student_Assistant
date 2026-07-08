using System.Net.Http.Headers;
using System.Text.Json;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Infrastructure.Ai.Extraction;

public class AiExtractionClient : IAiExtractionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiExtractionClient> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _timeout;

    public AiExtractionClient(
        HttpClient httpClient,
        ILogger<AiExtractionClient> logger,
        TimeSpan pollingInterval,
        TimeSpan timeout)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pollingInterval = pollingInterval;
        _timeout = timeout;
    }

    public async Task<JobAcceptedResponse> SubmitExtractionJobAsync(
        Stream pdfStream,
        string bookId,
        int? startPage = null,
        int? endPage = null,
        CancellationToken cancellationToken = default)
    {
        using var formData = new MultipartFormDataContent();
        
        var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formData.Add(fileContent, "file", "document.pdf");
        
        formData.Add(new StringContent(bookId), "book_id");
        if(startPage.HasValue)
           formData.Add(new StringContent(startPage.ToString()), "start_page");

        if(endPage.HasValue)
           formData.Add(new StringContent(endPage.ToString()), "end_page");

        var response = await _httpClient.PostAsync("/api/v1/extraction/extract-pdf-async", formData, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to submit extraction job: {response.StatusCode} - {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JobAcceptedResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize job accepted response");
    }

    public async Task<JobStatusResponse> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/extraction/jobs/{jobId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException($"Job {jobId} not found");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get job status: {response.StatusCode} - {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JobStatusResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize job status response");
    }

    public async Task<JobResultResponse> GetJobResultAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/extraction/jobs/{jobId}/result", cancellationToken);
        var errorContent = "";
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException($"Job {jobId} not found");
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                 errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Job is not ready yet: {errorContent}");
            }
            
             errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get job result: {response.StatusCode} - {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JobResultResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize job result response");
    }

    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesFromJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < _timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var status = await GetJobStatusAsync(jobId, cancellationToken);
            
            _logger.LogInformation("Job {JobId} status: {Status}, Pages: {PagesDone}/{PagesTotal}", 
                jobId, status.Status, status.PagesDone, status.PagesTotal);
            
            if (status.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
            {
                var result = await GetJobResultAsync(jobId, cancellationToken);
                
                // Convert PageResult to ExtractedPageDto
                var extractedPages = result.Pages
                    .Where(p => p.Success && (p.Concepts.Any() || !string.IsNullOrWhiteSpace(FormatConceptsAsText(p.Concepts))))
                    .Select(p => new ExtractedPageDto
                    {
                        PageNumber = p.PageNumber,
                        Text = FormatConceptsAsText(p.Concepts),
                        Concepts = p.Concepts.Select(c => new ExtractedConceptDto
                        {
                            Title = c.Title,
                            Content = c.Content,
                            Keywords = c.Keywords ?? new List<string>()
                        }).ToList()
                    })
                    .Where(p => !string.IsNullOrWhiteSpace(p.Text) || p.Concepts.Any())
                    .ToList();
                
                return extractedPages;
            }
            
            if (status.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Extraction job failed: {status.Message}");
            }
            
            // Polling interval
            await Task.Delay(_pollingInterval, cancellationToken);
        }
        
        throw new TimeoutException($"Extraction job {jobId} timed out after {_timeout.TotalMinutes} minutes");
    }

    private static string FormatConceptsAsText(List<ExtractedConcept> concepts)
    {
        var textParts = new List<string>();
        
        foreach (var concept in concepts)
        {
            textParts.Add($"## {concept.Title}");
            textParts.Add(concept.Content);
            
            if (concept.Keywords.Any())
            {
                textParts.Add($"Keywords: {string.Join(", ", concept.Keywords)}");
            }
            
            textParts.Add(string.Empty);
        }
        
        return string.Join("\n", textParts);
    }
}
