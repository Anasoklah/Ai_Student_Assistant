namespace SyrianStudyBot.Common.Services;

public class DocumentUploadOptions
{
    public string StorageRootPath { get; set; } = "storage/document-uploads";
    public long MaxAdminFileSizeBytes { get; set; } = 100 * 1024 * 1024;
}
