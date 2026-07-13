namespace SyrianStudyBot.Domain.Enums;

/// <summary>
/// Lifecycle state of a document's ingestion pipeline.
/// Set to <see cref="Processing"/> when the upload is accepted, then flipped
/// to <see cref="Ready"/> or <see cref="Failed"/> by the background worker.
/// </summary>
public enum DocumentStatus
{
    Processing = 0,
    Ready = 1,
    Failed = 2
}
