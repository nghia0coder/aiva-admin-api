namespace Aiva.Admin.Api.UseCases.Files.GetStatus;

public record FileStatusDTO(
    int FileId,
    int MetadataId,
    string Status,
    DateTime? QueuedAt,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessingCompletedAt,
    int RetryCount,
    string? ErrorMessage,
    int? PageCount,
    int? WordCount,
    string? DetectedLanguage,
    bool IsEmbedded,
    DateTime? EmbeddedAt);
