using System.ClientModel;
using System.Diagnostics;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class PdfVisionExtractorService : IPdfVisionExtractorService
{
    private readonly ChatClient _visionClient;
    private readonly ILogger<PdfVisionExtractorService> _logger;

    // Safety limit: don't process more than 30 pages per document
    private const int MaxPages = 30;

    public PdfVisionExtractorService(IConfiguration configuration, ILogger<PdfVisionExtractorService> logger)
    {
        _logger = logger;

        var apiKey = configuration["OpenRouter:ApiKey"]!;
        var model = configuration["OpenRouter:VisionModel"] ?? "meta-llama/llama-3.2-11b-vision-instruct:free";

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
        );

        _visionClient = client.GetChatClient(model);
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
                _logger.LogWarning("pdftoppm produced no images — check that poppler-utils is installed");
                throw new ArgumentNullException(nameof(pageImages) ,"pdf is empty");
            }

            if (pageImages.Count > MaxPages)
            {
                _logger.LogWarning("PDF has {Total} pages, processing only first {Max}", pageImages.Count, MaxPages);
                pageImages = pageImages.Take(MaxPages).ToList();
            }

            _logger.LogInformation("Vision extracting {Count} pages via LLM", pageImages.Count);

            var pageTexts = new List<ExtractedPageDto>();
            for (int i = 0; i < pageImages.Count; i++)
            {
                var pageNumber = i + 1;
                _logger.LogInformation("Processing page {Page}/{Total}", pageNumber, pageImages.Count);
                var text = await ExtractPageTextAsync(pageImages[i], cancellationToken);
                if (!string.IsNullOrWhiteSpace(text))
                    pageTexts.Add(new ExtractedPageDto
                    {
                        PageNumber = pageNumber, Text = text
                    });
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
            // 150 DPI: good balance between quality and image size for OCR
            Arguments = $"-r 150 -png \"{pdfPath}\" \"{outputPrefix}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

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
        var messages = new List<ChatMessage>
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    "You are processing a page from a science or math textbook. Extract everything on this page:\n" +
                    "1. All text exactly as written, preserving reading order.\n" +
                    "2. Math equations in plain text: use × for multiplication, ÷ for division, ² for squared, ³ for cubed, √ for square root.\n" +
                    "3. For any diagram, figure, or image: write [Figure: brief description of what it shows] in place of the image.\n" +
                    "4. For any table: extract it as plain text rows.\n" +
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
}
