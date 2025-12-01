namespace Aiva.Admin.Api.Web.Files.Upload;

public record UploadFileResponse(
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
    DateTime CreatedOnUtc);
