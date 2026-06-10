using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class TelegramCommandHandler(
    IConfiguration configuration,
    IRagPipelineService ragPipeline,
    IUserSessionService userSessions,
    IDocumentIngestionService ingestion) : ITelegramCommandHandler
{
    public async Task<string> HandleAsync(
        string text,
        long userId,
        CancellationToken cancellationToken)
    {
        // Telegram commands are text messages that usually start with "/".
        // Example: "/summary photosynthesis" becomes command="/summary", args="photosynthesis".
        var (command, args) = ParseCommand(text);

        return command switch
        {
            "/start" => GetStartMessage(),
            "/help" => GetHelpMessage(),
            "/subject" => await HandleSubjectCommandAsync(args, userId, cancellationToken),
            "/explain" => await HandleRagCommandAsync(args, "explain", userId, cancellationToken),
            "/summary" => await HandleRagCommandAsync(args, "summary", userId, cancellationToken),
            "/quiz" => await HandleRagCommandAsync(args, "quiz", userId, cancellationToken),
            "/upload" => await HandleInlineUploadCommandAsync(args, userId, cancellationToken),
            _ => await HandleDefaultQuestionAsync(text, args, userId, cancellationToken),
        };
    }

    private async Task<string> HandleDefaultQuestionAsync(
        string text,
        string args,
        long userId,
        CancellationToken cancellationToken)
    {
        // If the user sends normal text, treat it as an explain question.
        // If the user sends an unknown command, use its arguments as the question.
        var question = text.TrimStart().StartsWith("/")
            ? args
            : text;

        return await HandleRagCommandAsync(question, "explain", userId, cancellationToken);
    }

    private async Task<string> HandleSubjectCommandAsync(
        string args,
        long userId,
        CancellationToken cancellationToken)
    {
        var subject = args.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            await userSessions.SetSubjectAsync(userId, null, cancellationToken);
            return "Subject filter cleared. I'll now search across all subjects.";
        }

        await userSessions.SetSubjectAsync(userId, subject, cancellationToken);
        return $"Subject set to \"{subject}\". Your questions will now search within this subject.";
    }

    private async Task<string> HandleRagCommandAsync(
        string args,
        string mode,
        long userId,
        CancellationToken cancellationToken)
    {
        var question = args.Trim();
        if (string.IsNullOrWhiteSpace(question))
            return $"Please provide a topic. Example: /{mode} photosynthesis";

        var session = await userSessions.GetOrCreateAsync(userId, cancellationToken);
        return await ragPipeline.QueryAsync(question, mode, session.CurrentSubject, cancellationToken);
    }

    private async Task<string> HandleInlineUploadCommandAsync(
        string args,
        long userId,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin(userId))
            return "You are not authorized to upload documents.";

        if (!TryParseInlineUpload(args, out var upload, out var errorMessage))
            return errorMessage;

        return await IngestDocumentAsync(upload, cancellationToken);
    }

    private static bool TryParseInlineUpload(
        string args,
        out DocumentUpload upload,
        out string errorMessage)
    {
        upload = null!;
        errorMessage = string.Empty;

        var parts = args.Split('\n', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            errorMessage = """
                Wrong format. Use:
                /upload [subject] | [title]
                [document content]

                Example:
                /upload Biology | Photosynthesis Notes
                Photosynthesis is the process by which plants...
                """;
            return false;
        }

        if (!TryParseSubjectAndTitle(parts[0], out var subject, out var title))
        {
            errorMessage = "Missing | separator. Format: /upload [subject] | [title]";
            return false;
        }

        var content = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(content))
        {
            errorMessage = "Subject, title, and content cannot be empty.";
            return false;
        }

        upload = new DocumentUpload(subject, title, content);
        return true;
    }

    private static bool TryParseSubjectAndTitle(string header, out string subject, out string title)
    {
        subject = string.Empty;
        title = string.Empty;

        var parts = header.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        subject = parts[0];
        title = parts[1];
        return true;
    }

    private async Task<string> IngestDocumentAsync(
        DocumentUpload upload,
        CancellationToken cancellationToken)
    {
        var document = await ingestion.IngestAsync(upload.Title, upload.Subject, upload.Content, cancellationToken);
        return $"Document \"{document.Title}\" ingested successfully with {document.Chunks.Count} chunks.";
    }

    private bool IsAdmin(long userId) =>
        userId == configuration.GetValue<long>("Telegram:AdminUserId");

    private static (string command, string args) ParseCommand(string text)
    {
        var trimmed = text.Trim();
        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return (string.Empty, string.Empty);

        var command = RemoveBotNameSuffix(parts[0]).ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        return (command, args);
    }

    private static string RemoveBotNameSuffix(string command)
    {
        // Telegram group chats can send commands as "/help@YourBotName".
        // The bot-name suffix must not become part of the command name.
        var atIndex = command.IndexOf('@', StringComparison.Ordinal);
        return atIndex < 0 ? command : command[..atIndex];
    }

    private static string GetStartMessage() => """
        Welcome to SyrianStudyBot!

        I can help you study by answering questions from your uploaded study material.

        Use /help to see all available commands.
        """;

    private static string GetHelpMessage() => """
        Available commands:

        /explain [topic] - Get a clear explanation of a topic
        /summary [topic] - Get a structured bullet-point summary
        /quiz [topic]    - Generate 5 multiple choice questions

        /subject [name]  - Filter search to a specific subject (e.g. /subject Math)
        /subject         - Clear the subject filter (search all subjects)

        /help  - Show this message
        /start - Welcome message

        Admin only:
        /upload [subject] | [title]
        [document content]
        """;

    private sealed record DocumentUpload(string Subject, string Title, string Content);
}
