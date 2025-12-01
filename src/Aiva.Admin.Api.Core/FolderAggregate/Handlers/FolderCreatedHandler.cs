namespace Aiva.Admin.Api.Core.FolderAggregate.Handlers;

using Events;
using Interfaces;
using StorageAggregate;

/// <summary>
/// Handles FolderCreatedEvent by creating virtual folder in Azure Blob Storage
/// </summary>
public class FolderCreatedHandler(
    ILogger<FolderCreatedHandler> logger,
    IBlobStorageService blobStorageService,
    IReadRepository<Storage> storageRepository) : INotificationHandler<FolderCreatedEvent>
{
  public async ValueTask Handle(FolderCreatedEvent domainEvent, CancellationToken cancellationToken)
  {
    var folder = domainEvent.Folder;

    // Get the storage to find container name
    var storage = await storageRepository.GetByIdAsync(folder.StorageId, cancellationToken);
    if (storage is null)
    {
      logger.LogError(
        "Storage {StorageId} not found for folder {FolderId}",
        folder.StorageId,
        folder.Id);
      return;
    }

    if (!storage.IsContainerProvisioned)
    {
      logger.LogWarning(
        "Container not provisioned yet for storage {StorageId}. Folder {FolderId} will be created when files are uploaded.",
        folder.StorageId,
        folder.Id);
      return;
    }

    logger.LogInformation(
      "Creating virtual folder {BlobPrefix} in container {ContainerName}",
      folder.BlobPrefix,
      storage.ContainerName);

    var result = await blobStorageService.CreateVirtualFolderAsync(
      storage.ContainerName,
      folder.BlobPrefix,
      cancellationToken);

    if (result.IsSuccess)
    {
      logger.LogInformation(
        "Successfully created virtual folder {BlobPrefix} at {FolderUri}",
        folder.BlobPrefix,
        result.Value);
    }
    else
    {
      logger.LogError(
        "Failed to create virtual folder {BlobPrefix}: {Errors}",
        folder.BlobPrefix,
        string.Join(", ", result.Errors));
    }
  }
}
