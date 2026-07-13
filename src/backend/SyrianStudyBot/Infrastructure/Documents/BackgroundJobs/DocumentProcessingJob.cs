namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

public record DocumentProcessingJob(
    Guid DocumentId,
    string TempFilePath,
    string FileName,
    int? StartPage,
    int? EndPage,
    int? TocPage,
    int? TocPageEnd,
    Guid UploadedByUserId
);
