namespace Aiva.Admin.Api.UseCases.Files.Upload;

using Core.FolderAggregate;
using Core.StorageAggregate;

public record UploadFileCommand(
    StorageId StorageId,
    FolderId FolderId,
    string OriginalFileName,
    string Extension,
    string ContentType,
    long FileSizeBytes,
    Stream FileContent) : ICommand<Result<FileDTO>>;
