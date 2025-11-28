namespace Aiva.Admin.Api.Core.StorageAggregate.Handlers;

using Events;
using Interfaces;

/// <summary>
/// Handles StorageCreatedEvent by creating corresponding blob container in Azure Storage
/// </summary>
public class StorageCreatedHandler(
    ILogger<StorageCreatedHandler> logger,
    IBlobStorageService blobStorageService,
    IRepository<Storage> storageRepository) : INotificationHandler<StorageCreatedEvent>
{
  public async ValueTask Handle(StorageCreatedEvent domainEvent, CancellationToken cancellationToken)
  {
    var storage = domainEvent.Storage;

    logger.LogInformation(
        "Creating blob container {ContainerName} for storage {StorageId}",
        storage.ContainerName,
        storage.Id);

    var result = await blobStorageService.CreateContainerAsync(
        storage.ContainerName,
        cancellationToken);

    if (result.IsSuccess)
    {
      storage.MarkContainerProvisioned();
      await storageRepository.UpdateAsync(storage, cancellationToken);

      logger.LogInformation(
          "Successfully created blob container {ContainerName} at {ContainerUri}",
          storage.ContainerName,
          result.Value);
    }
    else
    {
      logger.LogError(
          "Failed to create blob container {ContainerName}: {Errors}",
          storage.ContainerName,
          string.Join(", ", result.Errors));
    }
  }
}
