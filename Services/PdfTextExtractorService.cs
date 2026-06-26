using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;
using UglyToad.PdfPig;

namespace SyrianStudyBot.Services;

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
        .Select(page => new ExtractedPageDto{
            PageNumber = page.Number,
            Text = string.Join(" ", page.GetWords().Select(word => word.Text))})
        .Where(page => !string.IsNullOrWhiteSpace(page.Text))
        .ToList();
}
}
