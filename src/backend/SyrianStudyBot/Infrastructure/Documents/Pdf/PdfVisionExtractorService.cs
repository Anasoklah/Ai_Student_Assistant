using System.ClientModel;
using System.Diagnostics;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Infrastructure.Documents.Pdf;

public class PdfVisionExtractorService : IPdfVisionExtractorService
{
    private readonly ChatClient? _visionClient;
    private readonly ILogger<PdfVisionExtractorService> _logger;

    private const int MaxPages = 200;

    public PdfVisionExtractorService(IConfiguration configuration, ILogger<PdfVisionExtractorService> logger)
    {
        _logger = logger;

        var apiKey = configuration["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Vision extraction is disabled because OpenRouter:ApiKey is missing.");
            return;
        }

        var configuredModel = configuration["OpenRouter:VisionModel"];
        var model = string.IsNullOrWhiteSpace(configuredModel)
            ? "openai/gpt-4o-mini"
            : configuredModel;

        try
        {
            var client = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
            );

            _visionClient = client.GetChatClient(model);
            _logger.LogInformation("Using vision model {Model} for PDF extraction.", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenRouter vision client for model {Model}.", model);
            throw;
        }
    }

    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"studybot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var pdfPath = Path.Combine(tempDir, "input.pdf");
        var outputPrefix = Path.Combine(tempDir, "page");

        try
        {
            await File.WriteAllBytesAsync(pdfPath, pdfBytes, cancellationToken);

            var pageImages = await ConvertPdfToPagesAsync(pdfPath, outputPrefix, cancellationToken);

            if (pageImages.Count == 0)
            {
                throw new BadRequestException("Vision extraction could not render the PDF into images. Ensure pdftoppm/poppler-utils is installed.");
            }

            if (pageImages.Count > MaxPages)
            {
                _logger.LogWarning("PDF has {Total} pages, processing only first {Max}", pageImages.Count, MaxPages);
                pageImages = pageImages.Take(MaxPages).ToList();
            }

            var pageTexts = new List<ExtractedPageDto>();
            for (int i = 0; i < pageImages.Count; i++)
            {
                var pageNumber = i + 1;
                var text = await ExtractPageTextAsync(pageImages[i], cancellationToken);
                if (!string.IsNullOrWhiteSpace(text))
                    pageTexts.Add(new ExtractedPageDto { PageNumber = pageNumber, Text = text });
            }

            return pageTexts;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static async Task<List<byte[]>> ConvertPdfToPagesAsync(
        string pdfPath, string outputPrefix, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "pdftoppm",
            Arguments = $"-r 150 -png \"{pdfPath}\" \"{outputPrefix}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new BadRequestException($"pdftoppm failed with exit code {process.ExitCode}: {stderr}{stdout}");
        }

        var dir = Path.GetDirectoryName(outputPrefix)!;
        var prefix = Path.GetFileName(outputPrefix);

        return Directory
            .GetFiles(dir, $"{prefix}-*.png")
            .OrderBy(f => f)
            .Select(f => File.ReadAllBytes(f))
            .ToList();
    }

    private async Task<string> ExtractPageTextAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (_visionClient is null)
        {
            throw new BadRequestException("Vision extraction is not configured. Set OpenRouter:ApiKey first.");
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart(
                        "You are processing a page from a science or math textbook. Extract everything on this page:\n" +
                        "1. All text exactly as written, preserving reading order.\n" +
                        "2. Math equations MUST use LaTeX notation: use $...$ for inline equations and $$...$$ for display/block equations.\n" +
                        "3. Chapter headings (الفصل / الوحدة / الباب): prefix with '# ' .\n" +
                        "4. Section or lesson headings (الدرس / موضوع): prefix with '## ' .\n" +
                        "5. For any diagram, figure, or image: write [Figure: brief description of what it shows] in its place.\n" +
                        "6. For any table: extract it as plain text rows.\n" +
                        "Output only the extracted content, no commentary."
                    ),
                    ChatMessageContentPart.CreateImagePart(
                        BinaryData.FromBytes(imageBytes), "image/png"
                    )
                )
            };

            var response = await _visionClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text;
        }
        catch (ClientResultException ex)
        {
            throw new BadRequestException($"OpenRouter vision request failed: {ex.Message}", ex);
        }
    }
}
