namespace Aiva.Admin.Api.Core.StorageAggregate;

public class Storage(StorageName StorageName, string? StorageDescription) : AuditableEntity<Storage, StorageId>, IAggregateRoot
{
  public StorageName StorageName { get; private set; } = StorageName;
  public string? StorageDescription { get; set; } = StorageDescription;
  public string ContainerName { get; set; } = string.Empty;

  public Storage UpdateName(StorageName newName)
  {
    if (StorageName == newName) return this;
    StorageName = newName;
    return this;
  }
}
