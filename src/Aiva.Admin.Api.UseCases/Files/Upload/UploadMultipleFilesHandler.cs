namespace Aiva.Admin.Api.UseCases.Files.Upload;

using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using Core.FolderAggregate;
using Core.Interfaces;
using Core.StorageAggregate;

public class UploadMultipleFilesHandler(
    IRepository<File> fileRepository,
    IRepository<FileMetadata> fileMetadataRepository,
    IReadRepository<Storage> storageRepository,
    IReadRepository<Folder> folderRepository,
    IBlobStorageService blobStorageService)
    : ICommandHandler<UploadMultipleFilesCommand, Result<IReadOnlyList<FileDTO>>>
{
  public async ValueTask<Result<IReadOnlyList<FileDTO>>> Handle(
      UploadMultipleFilesCommand command,
      CancellationToken cancellationToken)
  {
    if (command.Files.Count == 0)
    {
      return Result.Invalid(new ValidationError("Files", "No files provided for upload."));
    }

    // Validate all extensions first
    foreach (var fileInfo in command.Files)
    {
      if (!AllowedFileExtensions.IsAllowed(fileInfo.Extension))
      {
        return Result.Invalid(new ValidationError(
            "Extension",
            $"File extension '{fileInfo.Extension}' is not allowed for file '{fileInfo.OriginalFileName}'. Allowed: {string.Join(", ", AllowedFileExtensions.Extensions)}"));
      }
    }

    // Get storage and validate
    var storage = await storageRepository.GetByIdAsync(command.StorageId, cancellationToken);
    if (storage is null)
      return Result.NotFound($"Storage with ID {command.StorageId.Value} not found.");

    if (!storage.IsContainerProvisioned)
      return Result.Error("Storage container is not provisioned yet.");

    // Get folder and validate
    var folder = await folderRepository.GetByIdAsync(command.FolderId, cancellationToken);
    if (folder is null)
      return Result.NotFound($"Folder with ID {command.FolderId.Value} not found.");

    if (folder.StorageId != command.StorageId)
      return Result.Invalid(new ValidationError("FolderId", "Folder does not belong to the specified storage."));

    var uploadedFiles = new List<FileDTO>();
    var errors = new List<string>();

    // Process each file individually
    foreach (var fileInfo in command.Files)
    {
      try
      {
        var result = await ProcessSingleFile(
            fileInfo,
            command.StorageId,
            command.FolderId,
            storage,
            folder,
            cancellationToken);

        if (result.IsSuccess)
        {
          uploadedFiles.Add(result.Value);
        }
        else
        {
          errors.Add($"Failed to upload '{fileInfo.OriginalFileName}': {result.Errors.FirstOrDefault() ?? "Unknown error"}");
        }
      }
      catch (Exception ex)
      {
        errors.Add($"Exception uploading '{fileInfo.OriginalFileName}': {ex.Message}");
      }
    }

    // If all files failed, return error
    if (uploadedFiles.Count == 0)
    {
      return Result.Error(string.Join("; ", errors));
    }

    // If some files failed but some succeeded, we'll return success with uploaded files
    // Consider logging the partial failures
    if (errors.Count > 0)
    {
      // Log partial failures (you might want to add proper logging here)
      System.Diagnostics.Debug.WriteLine($"Partial upload failures: {string.Join("; ", errors)}");
    }

    return Result.Success(uploadedFiles.AsReadOnly() as IReadOnlyList<FileDTO>);
  }

  private async ValueTask<Result<FileDTO>> ProcessSingleFile(
      FileUploadInfo fileInfo,
      StorageId storageId,
      FolderId folderId,
      Storage storage,
      Folder folder,
      CancellationToken cancellationToken)
  {
    // Use sanitized original filename instead of GUID
    var storedFileName = fileInfo.OriginalFileName;

    // Build blob path: folderPath/fileName
    var blobPath = $"{folder.BlobPrefix}/{storedFileName}";

    // Upload to blob storage (overwrites if exists)
    var uploadResult = await blobStorageService.UploadFileAsync(
        storage.ContainerName,
        blobPath,
        fileInfo.FileContent,
        fileInfo.ContentType,
        cancellationToken);

    if (!uploadResult.IsSuccess)
      return Result.Error(uploadResult.Errors.ToString());

    // Check if file with same name exists in same folder
    var existingFileSpec = new FileByNameAndFolderSpec(folderId, fileInfo.OriginalFileName);
    var existingFile = await fileRepository.FirstOrDefaultAsync(existingFileSpec, cancellationToken);

    File savedFile;
    FileMetadata metadata;

    if (existingFile is not null)
    {
      // UPDATE existing file record
      existingFile.Update(
          fileInfo.ContentType,
          fileInfo.FileSizeBytes,
          storedFileName,
          blobPath,
          uploadResult.Value);

      await fileRepository.UpdateAsync(existingFile, cancellationToken);
      savedFile = existingFile;

      var metadataSpec = new FileMetadataByFileIdSpec(existingFile.Id);
      metadata = await fileMetadataRepository.FirstOrDefaultAsync(metadataSpec, cancellationToken)
                 ?? FileMetadata.Create(existingFile.Id);
    }
    else
    {
      // CREATE new file record
      var file = File.Create(
          FileName.From(fileInfo.OriginalFileName),
          fileInfo.Extension,
          fileInfo.ContentType,
          fileInfo.FileSizeBytes,
          storageId,
          folderId);

      file.SetBlobDetails(storedFileName, blobPath, uploadResult.Value);
      savedFile = await fileRepository.AddAsync(file, cancellationToken);

      // CREATE FileMetadata with status = Queued
      metadata = FileMetadata.Create(savedFile.Id);
      metadata.MarkAsQueued();
      await fileMetadataRepository.AddAsync(metadata, cancellationToken);
    }

    return new FileDTO(
        savedFile.Id.Value,
        savedFile.OriginalFileName.Value,
        savedFile.StoredFileName,
        savedFile.Extension,
        savedFile.ContentType,
        savedFile.FileSizeBytes,
        savedFile.BlobPath,
        savedFile.BlobUrl,
        savedFile.StorageId.Value,
        savedFile.FolderId.Value,
        metadata.Status.Name,
        metadata.QueuedAt,
        savedFile.CreatedOnUtc);
  }
}
