namespace Aiva.Admin.Api.Core.StorageAggregate.Events;

/// <summary>
/// Domain event dispatched when a new Storage is created.
/// Used to trigger blob container creation in Azure Storage.
/// </summary>
public sealed class StorageCreatedEvent(Storage storage) : DomainEventBase
{
  public Storage Storage { get; init; } = storage;
}
