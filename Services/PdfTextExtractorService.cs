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
        
            var pages = ExtractPagesWithPdfPig(pdfBytes);
            var totleCharacters = pages.Sum(page => page.Text.Length);
            if (!forceVision &&  totleCharacters >= 200)
                return pages;

            logger.LogInformation(
                "PdfPig got only {Chars} chars. PDF is likely image-based, switching to vision",
                totleCharacters);
        

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
