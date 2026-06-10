using SyrianStudyBot.interfaces;
using UglyToad.PdfPig;

namespace SyrianStudyBot.Services;

public class PdfTextExtractorService(
    ILogger<PdfTextExtractorService> logger,
    IPdfVisionExtractorService pdfVisionExtractor) : IPdfTextExtractorService
{
    private const int PdfTextFastPathMinCharacters = 200;

    public async Task<string> ExtractTextAsync(
        byte[] pdfBytes,
        bool forceVision,
        Func<Task> beforeVisionExtraction,
        CancellationToken cancellationToken = default)
    {
        if (!forceVision)
        {
            var pdfText = ExtractTextWithPdfPig(pdfBytes);
            if (pdfText.Length >= PdfTextFastPathMinCharacters)
                return pdfText;

            logger.LogInformation(
                "PdfPig got only {Chars} chars. PDF is likely image-based, switching to vision",
                pdfText.Length);
        }

        await beforeVisionExtraction();
        return await pdfVisionExtractor.ExtractTextAsync(pdfBytes, cancellationToken);
    }

    private static string ExtractTextWithPdfPig(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        var pages = pdf.GetPages()
            .Select(page => string.Join(" ", page.GetWords().Select(word => word.Text)));

        return string.Join("\n\n", pages);
    }
}
