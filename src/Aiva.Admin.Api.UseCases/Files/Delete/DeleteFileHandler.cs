namespace Aiva.Admin.Api.UseCases.Files.Delete;

using Ardalis.Result;
using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using Core.Interfaces;
using Core.StorageAggregate;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for deleting files from blob storage, Azure AI Search, and database
/// </summary>
public sealed class DeleteFileHandler(
    IRepository<File> fileRepository,
    IRepository<FileMetadata> fileMetadataRepository,
    IReadRepository<Storage> storageRepository,
    IBlobStorageService blobStorageService,
    IVectorStoreService vectorStoreService,
    IVectorStoreSettings vectorStoreConfig,
    ILogger<DeleteFileHandler> logger)
    : ICommandHandler<DeleteFileCommand, Result>
{
  public async ValueTask<Result> Handle(
      DeleteFileCommand command,
      CancellationToken cancellationToken)
  {
    // 1. Get file from database
    var file = await fileRepository.GetByIdAsync(command.FileId, cancellationToken);
    if (file is null)
    {
      return Result.NotFound($"File {command.FileId.Value} not found");
    }

    var errors = new List<string>();
    var hasError = false;

    // 2. Delete from Azure AI Search (if embedded)
    var metadataSpec = new FileMetadataByFileIdSpec(file.Id);
    var metadata = await fileMetadataRepository.FirstOrDefaultAsync(metadataSpec, cancellationToken);

    if (metadata?.IsEmbedded == true)
    {
      try
      {
        var collectionName = vectorStoreConfig.DefaultCollectionName;
        var deleteVectorResult = await vectorStoreService.DeleteByDocumentIdAsync(
            collectionName,
            file.Id.Value.ToString(),
            cancellationToken);

        if (!deleteVectorResult.IsSuccess)
        {
          var errorMsg = $"Failed to delete from Azure AI Search: {string.Join(", ", deleteVectorResult.Errors)}";
          errors.Add(errorMsg);
          hasError = true;
          logger.LogError("{ErrorMessage}", errorMsg);
        }
        else
        {
          logger.LogInformation(
              "Deleted file {FileId} chunks from Azure AI Search index {CollectionName}",
              file.Id.Value,
              collectionName);
        }
      }
      catch (Exception ex)
      {
        var errorMsg = $"Exception deleting from Azure AI Search: {ex.Message}";
        errors.Add(errorMsg);
        hasError = true;
        logger.LogError(ex, "Failed to delete file {FileId} from Azure AI Search", file.Id.Value);
      }
    }

    // 3. Delete from Blob Storage
    if (!string.IsNullOrWhiteSpace(file.BlobPath))
    {
      try
      {
        // Get storage to retrieve container name
        var storage = await storageRepository.GetByIdAsync(file.StorageId, cancellationToken);
        if (storage is null)
        {
          var errorMsg = $"Storage {file.StorageId.Value} not found";
          errors.Add(errorMsg);
          hasError = true;
          logger.LogWarning("{ErrorMessage}", errorMsg);
        }
        else
        {
          var deleteBlobResult = await blobStorageService.DeleteFileAsync(
              storage.ContainerName,
              file.BlobPath,
              cancellationToken);

          if (!deleteBlobResult.IsSuccess)
          {
            var errorMsg = $"Failed to delete from Blob Storage: {string.Join(", ", deleteBlobResult.Errors)}";
            errors.Add(errorMsg);
            hasError = true;
            logger.LogError("{ErrorMessage}", errorMsg);
          }
          else
          {
            logger.LogInformation(
                "Deleted file {FileId} from Blob Storage: {BlobPath}",
                file.Id.Value,
                file.BlobPath);
          }
        }
      }
      catch (Exception ex)
      {
        var errorMsg = $"Exception deleting from Blob Storage: {ex.Message}";
        errors.Add(errorMsg);
        hasError = true;
        logger.LogError(ex, "Failed to delete file {FileId} from Blob Storage", file.Id.Value);
      }
    }

    // 4. Delete metadata from database
    if (metadata is not null)
    {
      try
      {
        await fileMetadataRepository.DeleteAsync(metadata, cancellationToken);
        logger.LogInformation("Deleted FileMetadata for file {FileId}", file.Id.Value);
      }
      catch (Exception ex)
      {
        var errorMsg = $"Failed to delete file metadata: {ex.Message}";
        errors.Add(errorMsg);
        hasError = true;
        logger.LogError(ex, "Failed to delete FileMetadata for file {FileId}", file.Id.Value);
      }
    }

    // 5. Delete file record from database
    try
    {
      await fileRepository.DeleteAsync(file, cancellationToken);
      logger.LogInformation("Deleted File record for file {FileId}", file.Id.Value);
    }
    catch (Exception ex)
    {
      var errorMsg = $"Failed to delete file record: {ex.Message}";
      errors.Add(errorMsg);
      hasError = true;
      logger.LogError(ex, "Failed to delete File record for file {FileId}", file.Id.Value);
    }

    // Return result
    if (hasError)
    {
      return errors.Count > 0
          ? Result.Error(string.Join("; ", errors))
          : Result.Error("Failed to delete file completely");
    }

    logger.LogInformation(
        "Successfully deleted file {FileId} from all systems (Blob Storage, Azure AI Search, Database)",
        file.Id.Value);

    return Result.Success();
  }
}
