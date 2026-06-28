using System.Text;
using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class DocumentFileExtractionService(
    IPdfTextExtractorService pdfTextExtractor,
    ILogger<DocumentFileExtractionService> logger) : IDocumentFileExtractionService
{
    private const string PdfExtension = ".pdf";
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md"
    };

    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        IFormFile file,
        bool forceVision,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);

        if (string.Equals(extension, PdfExtension, StringComparison.OrdinalIgnoreCase))
            return await ExtractPdfPagesAsync(file, forceVision, cancellationToken);

        if (TextExtensions.Contains(extension))
            return await ExtractTextFileAsync(file, cancellationToken);

        throw new BadRequestException("Only PDF and text files are supported.");
    }

    private async Task<IReadOnlyList<ExtractedPageDto>> ExtractPdfPagesAsync(
        IFormFile file,
        bool forceVision,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (!LooksLikePdf(bytes))
            throw new BadRequestException("The uploaded file is not a valid PDF.");

        return await pdfTextExtractor.ExtractPagesAsync(
            bytes,
            forceVision,
            () =>
            {
                logger.LogInformation("Switching PDF '{FileName}' to vision extraction.", file.FileName);
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private static async Task<IReadOnlyList<ExtractedPageDto>> ExtractTextFileAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);

        var text = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return [new ExtractedPageDto { PageNumber = 1, Text = text }];
    }

    private static bool LooksLikePdf(byte[] bytes)
    {
        return bytes.Length >= 4
            && bytes[0] == '%'
            && bytes[1] == 'P'
            && bytes[2] == 'D'
            && bytes[3] == 'F';
    }
}
