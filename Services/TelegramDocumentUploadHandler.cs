using System.Text;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SyrianStudyBot.Services;

public class TelegramDocumentUploadHandler(
    IConfiguration configuration,
    IDocumentIngestionService ingestion,
    IPdfTextExtractorService pdfTextExtractor) : ITelegramDocumentUploadHandler
{
    private const string VisionCaptionFlag = "vision";

    public async Task HandleAsync(
        ITelegramBotClient botClient,
        Message message,
        Document document,
        long chatId,
        long userId,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin(userId))
        {
            await botClient.SendMessage(chatId, "You are not authorized to upload documents.", cancellationToken: cancellationToken);
            return;
        }

        if (!TryParseFileCaption(message.Caption, out var caption, out var captionError))
        {
            await botClient.SendMessage(chatId, captionError, cancellationToken: cancellationToken);
            return;
        }

        if (!TryGetSupportedFileKind(document.FileName, out var fileKind, out var fileError))
        {
            await botClient.SendMessage(chatId, fileError, cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(chatId, "Downloading and processing your document, please wait...", cancellationToken: cancellationToken);

        var fileBytes = await DownloadTelegramFileAsync(botClient, document.FileId, cancellationToken);
        var request = await BuildIngestionRequestAsync(
            fileBytes,
            fileKind,
            caption,
            chatId,
            botClient,
            cancellationToken);

        if (request.Pages.Count == 0 || request.Pages.All(page => string.IsNullOrWhiteSpace(page.Text)))
        {
            await botClient.SendMessage(chatId, "Could not extract any text from the file.", cancellationToken: cancellationToken);
            return;
        }

        var uploadedDocument = await ingestion.IngestAsync(request, cancellationToken);

        await botClient.SendMessage(
            chatId,
            $"Document \"{uploadedDocument.Title}\" ingested successfully with {uploadedDocument.Chunks.Count} chunks.",
            cancellationToken: cancellationToken);
    }

    private static bool TryParseFileCaption(
        string? caption,
        out FileUploadCaption uploadCaption,
        out string errorMessage)
    {
        uploadCaption = null!;
        errorMessage = string.Empty;

        // Telegram stores the text under an attached file as Message.Caption.
        // For this bot, the caption tells us the subject, title, and optional PDF vision mode.
        if (!TryParseSubjectAndTitle(caption ?? string.Empty, out var subject, out var title, out var flag))
        {
            errorMessage =
                "Please add a caption to your file: [subject] | [title]\n" +
                "For image-heavy PDFs (equations/scanned): [subject] | [title] | vision\n\n" +
                "Example: Physics | Newton's Laws\n" +
                "Example: Physics | Newton's Laws | vision";

            return false;
        }

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(title))
        {
            errorMessage = "Subject and title cannot be empty.";
            return false;
        }

        var forceVision = string.Equals(flag, VisionCaptionFlag, StringComparison.OrdinalIgnoreCase);
        uploadCaption = new FileUploadCaption(subject, title, forceVision);
        return true;
    }

    private static bool TryParseSubjectAndTitle(
        string header,
        out string subject,
        out string title,
        out string? flag)
    {
        subject = string.Empty;
        title = string.Empty;
        flag = null;

        var parts = header.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        subject = parts[0];
        title = parts[1];
        flag = parts.Length > 2 ? parts[2] : null;
        return true;
    }

    private static bool TryGetSupportedFileKind(
        string? fileName,
        out UploadedFileKind fileKind,
        out string errorMessage)
    {
        fileKind = UploadedFileKind.Unsupported;
        errorMessage = string.Empty;

        if (fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
        {
            fileKind = UploadedFileKind.Pdf;
            return true;
        }

        if (fileName?.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) == true)
        {
            fileKind = UploadedFileKind.Text;
            return true;
        }

        errorMessage = "Only PDF and TXT files are supported.";
        return false;
    }

    private static async Task<byte[]> DownloadTelegramFileAsync(
        ITelegramBotClient botClient,
        string fileId,
        CancellationToken cancellationToken)
    {
        // A Telegram Document gives us a FileId first.
        // GetFile turns that id into a temporary Telegram file path, then DownloadFile streams the bytes.
        var fileInfo = await botClient.GetFile(fileId, cancellationToken);

        using var stream = new MemoryStream();
        await botClient.DownloadFile(fileInfo.FilePath!, stream, cancellationToken);
        return stream.ToArray();
    }

    private async Task<DocumentIngestionRequestDto> BuildIngestionRequestAsync(
        byte[] fileBytes,
        UploadedFileKind fileKind,
        FileUploadCaption caption,
        long chatId,
        ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        var pages = fileKind switch
        {
            UploadedFileKind.Pdf => await pdfTextExtractor.ExtractPagesAsync(
                fileBytes,
                caption.ForceVision,
                () => NotifyVisionExtractionAsync(botClient, chatId, cancellationToken),
                cancellationToken),

            UploadedFileKind.Text =>
            [
                new ExtractedPageDto
                {
                    PageNumber = 1,
                    Text = Encoding.UTF8.GetString(fileBytes)
                }
            ],

            _ => []
        };

        return new DocumentIngestionRequestDto
        {
            Title = caption.Title,
            Subject = caption.Subject,
            SourceName = caption.Title,
            Language = "Arabic",
            Pages = pages
        };
    }

    private static Task NotifyVisionExtractionAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken) =>
        botClient.SendMessage(
            chatId,
            "This PDF appears to contain images or scanned content. Running vision extraction, this may take a few minutes...",
            cancellationToken: cancellationToken);

    private bool IsAdmin(long userId) =>
        userId == configuration.GetValue<long>("Telegram:AdminUserId");

    private sealed record FileUploadCaption(string Subject, string Title, bool ForceVision);

    private enum UploadedFileKind
    {
        Unsupported,
        Pdf,
        Text
    }
}
