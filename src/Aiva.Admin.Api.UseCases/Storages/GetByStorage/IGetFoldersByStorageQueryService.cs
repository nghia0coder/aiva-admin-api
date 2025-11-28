namespace Aiva.Admin.Api.UseCases.Folders.GetByStorage;

using Core.StorageAggregate;

/// <summary>
/// Query service for fetching folders by storage - optimized for hierarchical data
/// </summary>
public interface IGetFoldersByStorageQueryService
{
  /// <summary>
  /// Gets flat list of all folders for a storage (single DB round-trip)
  /// </summary>
  Task<IReadOnlyList<FolderDto>> GetFlatListAsync(StorageId storageId, CancellationToken ct = default);

  /// <summary>
  /// Gets folders as a tree structure (optimized: single query + in-memory tree building)
  /// </summary>
  Task<IReadOnlyList<FolderTreeNodeDto>> GetTreeAsync(StorageId storageId, CancellationToken ct = default);
}
