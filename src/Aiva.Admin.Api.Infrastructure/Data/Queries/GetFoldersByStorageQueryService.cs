namespace Aiva.Admin.Api.Infrastructure.Data.Queries;

using Core.FolderAggregate;
using Core.StorageAggregate;
using UseCases.Folders;
using UseCases.Folders.GetByStorage;

public class GetFoldersByStorageQueryService(AppDbContext db) : IGetFoldersByStorageQueryService
{
  /// <summary>
  /// Single query to get all folders for a storage - uses existing index on StorageId
  /// </summary>
  public async Task<IReadOnlyList<FolderDto>> GetFlatListAsync(StorageId storageId, CancellationToken ct = default)
  {
    return await db.Folders
      .Where(f => f.StorageId == storageId)
      .OrderBy(f => f.BlobPrefix) // Natural hierarchical ordering by path
      .Select(f => new FolderDto(
        f.Id,
        f.Name,
        f.Description,
        f.BlobPrefix,
        f.StorageId,
        f.ParentFolderId,
        f.CreatedOnUtc))
      .AsNoTracking()
      .ToListAsync(ct);
  }

  /// <summary>
  /// Gets folders as tree - optimized single DB query + O(n) in-memory tree building
  /// </summary>
  public async Task<IReadOnlyList<FolderTreeNodeDto>> GetTreeAsync(StorageId storageId, CancellationToken ct = default)
  {
    // Single query - fetches ALL folders for the storage
    var folders = await db.Folders
      .Where(f => f.StorageId == storageId)
      .AsNoTracking()
      .ToListAsync(ct);

    // Only if performance becomes an issue with very large datasets
    // var folders = await db.Folders
    //   .FromSqlRaw(@"
    //     WITH FolderHierarchy AS (
    //       SELECT * FROM Folders WHERE StorageId = {0} AND ParentFolderId IS NULL
    //       UNION ALL
    //       SELECT f.* FROM Folders f
    //       INNER JOIN FolderHierarchy h ON f.ParentFolderId = h.Id
    //     )
    //     SELECT * FROM FolderHierarchy", storageId.Value)
    //   .AsNoTracking()
    //   .ToListAsync(ct);

    return BuildTree(folders);
  }

  /// <summary>
  /// O(n) tree building algorithm using dictionary lookup
  /// </summary>
  private static IReadOnlyList<FolderTreeNodeDto> BuildTree(List<Folder> folders)
  {
    if (folders.Count == 0)
      return [];

    // Create lookup dictionary - O(n)
    var lookup = folders.ToDictionary(
      f => f.Id,
      f => new FolderTreeNodeDto(f.Id, f.Name, f.Description, f.BlobPrefix, []));

    var roots = new List<FolderTreeNodeDto>();

    // Build tree structure - O(n)
    foreach (var folder in folders)
    {
      var node = lookup[folder.Id];

      if (folder.ParentFolderId.HasValue && lookup.TryGetValue(folder.ParentFolderId.Value, out var parent))
      {
        parent.Children.Add(node);
      }
      else
      {
        roots.Add(node);
      }
    }

    return roots;
  }
}
