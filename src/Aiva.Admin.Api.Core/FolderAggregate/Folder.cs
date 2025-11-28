using Aiva.Admin.Api.Core.StorageAggregate;

namespace Aiva.Admin.Api.Core.FolderAggregate;

public class Folder : AuditableEntity<Folder, FolderId>, IAggregateRoot
{
  public FolderName Name { get; private set; }
  public string? Description { get; private set; }

  /// <summary>
  /// Virtual path in blob storage (e.g., "documents/2024/reports")
  /// This corresponds to blob prefix in Azure Storage
  /// </summary>
  public string BlobPrefix { get; private set; } = string.Empty;

  /// <summary>
  /// Reference to parent Storage (by ID only - no navigation)
  /// </summary>
  public StorageId StorageId { get; private set; }

  /// <summary>
  /// Self-referencing for folder hierarchy
  /// </summary>
  public FolderId? ParentFolderId { get; private set; }

  // Required by EF Core
  private Folder() { }

  private Folder(FolderName name, StorageId storageId, FolderId? parentFolderId, string? description)
  {
    Name = name;
    StorageId = storageId;
    ParentFolderId = parentFolderId;
    Description = description;
  }

  /// <summary>
  /// Factory method to create a new Folder
  /// </summary>
  public static Folder Create(
    FolderName name,
    StorageId storageId,
    FolderId? parentFolderId = null,
    string? description = null)
  {
    var folder = new Folder(name, storageId, parentFolderId, description);
    return folder;
  }

  /// <summary>
  /// Set the blob prefix after folder is persisted and has an ID
  /// </summary>
  public void SetBlobPrefix(string? parentPrefix = null)
  {
    BlobPrefix = string.IsNullOrEmpty(parentPrefix)
      ? Name.Value
      : $"{parentPrefix}/{Name.Value}";

    //RegisterDomainEvent(new FolderCreatedEvent(this));
  }

  public Folder UpdateName(FolderName newName)
  {
    if (Name == newName) return this;

    var oldName = Name;
    Name = newName;

    //RegisterDomainEvent(new FolderNameUpdatedEvent(this, oldName));
    return this;
  }

  public Folder UpdateDescription(string? description)
  {
    Description = description;
    return this;
  }

  public void Move(FolderId? newParentFolderId, string newBlobPrefix)
  {
    ParentFolderId = newParentFolderId;
    BlobPrefix = newBlobPrefix;
    //RegisterDomainEvent(new FolderMovedEvent(this));
  }
}
