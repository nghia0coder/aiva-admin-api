namespace Aiva.Admin.Api.UseCases.Files;

public record FileDTO(
    int Id,
    string OriginalFileName,
    string StoredFileName,
    string Extension,
    string ContentType,
    long FileSizeBytes,
    string BlobPath,
    string BlobUrl,
    int StorageId,
    int FolderId,
    string FileProcessingStatus,
    DateTime? QueuedAt,
    DateTime CreatedOnUtc);
