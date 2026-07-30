namespace SyrianStudyBot.Application.Documents;

/// <summary>
/// Data required by the background processor. The temporary file is owned by
/// Infrastructure and is deleted after processing completes.
/// </summary>
public record DocumentProcessingRequest(
    Guid DocumentId,
    string TempFilePath,
    string FileName,
    int? StartPage,
    int? EndPage,
    int? TocPage,
    int? TocPageEnd,
    Guid? UploadedByUserId
);
