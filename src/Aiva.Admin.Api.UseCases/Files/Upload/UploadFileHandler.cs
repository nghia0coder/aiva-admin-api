namespace Aiva.Admin.Api.UseCases.Files.Upload;

using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using Core.FolderAggregate;
using Core.Interfaces;
using Core.StorageAggregate;

public class UploadFileHandler(
    IRepository<File> fileRepository,
    IReadRepository<Storage> storageRepository,
    IReadRepository<Folder> folderRepository,
    IBlobStorageService blobStorageService)
    : ICommandHandler<UploadFileCommand, Result<FileDTO>>
{
  public async ValueTask<Result<FileDTO>> Handle(
      UploadFileCommand command,
      CancellationToken cancellationToken)
  {
    // Validate extension
    if (!AllowedFileExtensions.IsAllowed(command.Extension))
    {
      return Result.Invalid(new ValidationError(
          "Extension",
          $"File extension '{command.Extension}' is not allowed. Allowed: {string.Join(", ", AllowedFileExtensions.Extensions)}"));
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

    // Use sanitized original filename instead of GUID
    var storedFileName = command.OriginalFileName;

    // Build blob path: folderPath/fileName
    var blobPath = $"{folder.BlobPrefix}/{storedFileName}";

    // Upload to blob storage (overwrites if exists)
    var uploadResult = await blobStorageService.UploadFileAsync(
        storage.ContainerName,
        blobPath,
        command.FileContent,
        command.ContentType,
        cancellationToken);

    if (!uploadResult.IsSuccess)
      return Result.Error(uploadResult.Errors.ToString());

    // Check if file with same name exists in same folder
    var existingFileSpec = new FileByNameAndFolderSpec(command.FolderId, command.OriginalFileName);
    var existingFile = await fileRepository.FirstOrDefaultAsync(existingFileSpec, cancellationToken);

    File savedFile;

    if (existingFile is not null)
    {
      // UPDATE existing file record
      existingFile.Update(
          command.ContentType,
          command.FileSizeBytes,
          storedFileName,
          blobPath,
          uploadResult.Value);

      await fileRepository.UpdateAsync(existingFile, cancellationToken);
      savedFile = existingFile;
    }
    else
    {
      // CREATE new file record
      var file = File.Create(
          FileName.From(command.OriginalFileName),
          command.Extension,
          command.ContentType,
          command.FileSizeBytes,
          command.StorageId,
          command.FolderId);

      file.SetBlobDetails(storedFileName, blobPath, uploadResult.Value);
      savedFile = await fileRepository.AddAsync(file, cancellationToken);
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
        savedFile.CreatedOnUtc);
  }

  /// <summary>
  /// Sanitize filename to be safe for Azure Blob Storage
  /// </summary>
  private static string SanitizeFileName(string fileName)
  {
    // Characters invalid in Azure Blob names or URLs
    var invalidChars = Path.GetInvalidFileNameChars()
        .Concat(new[] { '#', '?', '%', '&', '+' })
        .ToArray();

    var sanitized = string.Join("_", fileName.Split(invalidChars));

    // Replace spaces with underscores
    sanitized = sanitized.Replace(" ", "_");

    // Remove consecutive underscores
    while (sanitized.Contains("__"))
      sanitized = sanitized.Replace("__", "_");

    // Limit length (Azure max blob name segment: ~255 chars)
    if (sanitized.Length > 200)
      sanitized = sanitized[..200];

    return sanitized.Trim('_');
  }
}
