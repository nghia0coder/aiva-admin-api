namespace Aiva.Admin.Api.Core.StorageAggregate;

public class Storage(StorageName storageName) : AuditableEntity<Storage, StorageId>, IAggregateRoot
{
  public StorageName StorageName { get; private set; } = storageName;
  public string StorageDescription { get; set; } = string.Empty;
  public string ContainerName { get; set; } = string.Empty;

  public Storage UpdateName(StorageName newName)
  {
    if (StorageName == newName) return this;
    StorageName = newName;
    return this;
  }
}
