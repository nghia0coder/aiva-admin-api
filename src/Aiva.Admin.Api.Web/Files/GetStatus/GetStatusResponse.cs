namespace Aiva.Admin.Api.Web.Files.GetStatus;

public record GetStatusResponse(
    int FileId,
    int MetadataId,
    string Status,
    DateTime? QueuedAt,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessingCompletedAt,
    int RetryCount,
    string? ErrorMessage,
    ProcessingMetadataInfo? Metadata);

public record ProcessingMetadataInfo(
    int? PageCount,
    int? WordCount,
    string? DetectedLanguage,
    bool IsEmbedded,
    DateTime? EmbeddedAt);
