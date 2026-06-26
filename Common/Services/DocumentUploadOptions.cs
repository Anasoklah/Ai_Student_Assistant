namespace SyrianStudyBot.Common.Services;

public class DocumentUploadOptions
{
    public long MaxAdminFileSizeBytes { get; set; } = 100 * 1024 * 1024;
}
