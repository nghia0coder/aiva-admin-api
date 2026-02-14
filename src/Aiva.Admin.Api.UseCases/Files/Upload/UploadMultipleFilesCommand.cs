namespace Aiva.Admin.Api.UseCases.Files.Upload;

using Core.FolderAggregate;
using Core.StorageAggregate;

public record FileUploadInfo(
    string OriginalFileName,
    string Extension,
    string ContentType,
    long FileSizeBytes,
    Stream FileContent);

public record UploadMultipleFilesCommand(
    StorageId StorageId,
    FolderId FolderId,
    IReadOnlyList<FileUploadInfo> Files) : ICommand<Result<IReadOnlyList<FileDTO>>>;
