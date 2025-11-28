using Aiva.Admin.Api.Core.StorageAggregate.Events;

namespace Aiva.Admin.Api.Core.StorageAggregate;

public class Storage(StorageName StorageName, string? StorageDescription) : AuditableEntity<Storage, StorageId>, IAggregateRoot
{
  public StorageName StorageName { get; private set; } = StorageName;
  public string? StorageDescription { get; set; } = StorageDescription;
  public string ContainerName { get; set; } = string.Empty;
  public bool IsContainerProvisioned { get; private set; } = false;

  /// <summary>
  /// Factory method to create a new Storage and raise domain event
  /// </summary>
  public static Storage Create(StorageName storageName, string? description)
  {
    var storage = new Storage(storageName, description);
    return storage;
  }

  public Storage UpdateName(StorageName newName)
  {
    if (StorageName == newName) return this;
    StorageName = newName;
    return this;
  }

  public void MarkContainerProvisioned()
  {
    IsContainerProvisioned = true;
  }

  public void SetContainerName()
  {
    ContainerName = $"container-{Id.Value}";
    RegisterDomainEvent(new StorageCreatedEvent(this));
  }
}
