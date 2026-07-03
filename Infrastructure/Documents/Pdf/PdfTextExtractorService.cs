using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SyrianStudyBot.Infrastructure.Documents.Pdf;

public class PdfTextExtractorService(
    ILogger<PdfTextExtractorService> logger,
    IPdfVisionExtractorService pdfVisionExtractor) : IPdfTextExtractorService
{
    private const int PdfTextFastPathMinCharacters = 200;

    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        byte[] pdfBytes,
        bool forceVision,
        Func<Task> beforeVisionExtraction,
        CancellationToken cancellationToken = default)
    {
        if (!forceVision)
        {
            try
            {
                var pages = ExtractPagesWithPdfPig(pdfBytes);
                var totalCharacters = pages.Sum(page => page.Text.Length);
                if (totalCharacters >= PdfTextFastPathMinCharacters)
                    return pages;

                logger.LogInformation(
                    "PdfPig got only {Chars} chars. PDF is likely image-based, switching to vision",
                    totalCharacters);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "PdfPig could not extract text, switching to vision extraction");
            }
        }

        await beforeVisionExtraction();
        return await pdfVisionExtractor.ExtractTextAsync(pdfBytes, cancellationToken);
    }

    private static List<ExtractedPageDto> ExtractPagesWithPdfPig(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);

        return pdf.GetPages()
            .Select(page => new ExtractedPageDto
            {
                PageNumber = page.Number,
                Text = ReconstructPageLines(page)
            })
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .ToList();
    }

    private static string ReconstructPageLines(Page page)
    {
        const double lineGroupingTolerance = 4.0;

        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        if (words.Count == 0) return string.Empty;

        var lines = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom / lineGroupingTolerance))
            .OrderByDescending(g => g.Key)
            .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));

        return string.Join("\n", lines);
    }
}
