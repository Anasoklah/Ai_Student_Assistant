namespace SyrianStudyBot.Application.Documents.Configuration;

public class DocumentUploadOptions
{
    public long MaxAdminFileSizeBytes { get; set; } = 500 * 1024 * 1024;
}
