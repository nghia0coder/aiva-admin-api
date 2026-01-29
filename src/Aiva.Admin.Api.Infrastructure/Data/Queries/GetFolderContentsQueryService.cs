namespace Aiva.Admin.Api.Infrastructure.Data.Queries;

using Core.FileAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;
using Microsoft.EntityFrameworkCore;
using UseCases.Folders.GetContents;

public class GetFolderContentsQueryService(AppDbContext db) : IGetFolderContentsQueryService
{
  public async Task<GetFolderContentsResult> GetContentsAsync(
    FolderId? folderId,
    StorageId? storageId,
    string? searchQuery,
    string sortBy,
    string sortOrder,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    // Determine the storage ID
    StorageId effectiveStorageId;
    if (folderId.HasValue)
    {
      // Get storage ID from folder
      effectiveStorageId = await db.Folders
        .Where(f => f.Id == folderId.Value)
        .Select(f => f.StorageId)
        .FirstOrDefaultAsync(cancellationToken);
    }
    else
    {
      effectiveStorageId = storageId!.Value;
    }

    // Build breadcrumbs
    var breadcrumbs = await BuildBreadcrumbsAsync(folderId, effectiveStorageId, cancellationToken);

    // Query subfolders
    var foldersQuery = db.Folders.AsQueryable();

    if (folderId.HasValue)
    {
      // Get direct children of the folder
      foldersQuery = foldersQuery.Where(f => f.ParentFolderId == folderId.Value);
    }
    else
    {
      // Get root level folders (no parent) for the storage
      foldersQuery = foldersQuery.Where(f => f.StorageId == effectiveStorageId && f.ParentFolderId == null);
    }

    // Apply search filter to folders
    if (!string.IsNullOrWhiteSpace(searchQuery))
    {
      var lowerSearchQuery = searchQuery.ToLower();
      foldersQuery = foldersQuery.Where(f =>
        EF.Property<string>(f, "Name").ToLower().Contains(lowerSearchQuery) ||
        (f.Description != null && f.Description.ToLower().Contains(lowerSearchQuery)));
    }

    // Query files
    var filesQuery = db.Files.AsQueryable();

    if (folderId.HasValue)
    {
      filesQuery = filesQuery.Where(f => f.FolderId == folderId.Value);
    }
    else
    {
      // Get files in root level folders (folders with no parent)
      var rootFolderIds = await db.Folders
        .Where(f => f.StorageId == effectiveStorageId && f.ParentFolderId == null)
        .Select(f => f.Id)
        .ToListAsync(cancellationToken);

      filesQuery = filesQuery.Where(f => rootFolderIds.Contains(f.FolderId));
    }

    // Apply search filter to files
    if (!string.IsNullOrWhiteSpace(searchQuery))
    {
      var lowerSearchQuery = searchQuery.ToLower();
      filesQuery = filesQuery.Where(f =>
        EF.Property<string>(f, "OriginalFileName").ToLower().Contains(lowerSearchQuery));
    }

    // Get counts for pagination
    var folderCount = await foldersQuery.CountAsync(cancellationToken);
    var fileCount = await filesQuery.CountAsync(cancellationToken);
    var totalCount = folderCount + fileCount;

    // Apply sorting to folders
    foldersQuery = ApplyFolderSorting(foldersQuery, sortBy, sortOrder);

    // Apply sorting to files
    filesQuery = ApplyFileSorting(filesQuery, sortBy, sortOrder);

    // Get folders first without the correlated subqueries
    var folderIds = await foldersQuery
      .Select(f => f.Id)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    // Get the folder details
    var folders = await db.Folders
      .Where(f => folderIds.Contains(f.Id))
      .Select(f => new
      {
        f.Id,
        Name = EF.Property<string>(f, "Name"),
        f.Description,
        f.BlobPrefix,
        f.CreatedOnUtc,
        f.ModifiedOnUtc
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    // Get subfolder counts
    var subfolderCounts = await db.Folders
      .Where(f => folderIds.Contains(f.ParentFolderId!.Value))
      .GroupBy(f => f.ParentFolderId!.Value)
      .Select(g => new { FolderId = g.Key, Count = g.Count() })
      .ToDictionaryAsync(x => x.FolderId, x => x.Count, cancellationToken);

    // Get file counts and sizes
    var fileStats = await db.Files
      .Where(f => folderIds.Contains(f.FolderId))
      .GroupBy(f => f.FolderId)
      .Select(g => new 
      { 
        FolderId = g.Key, 
        Count = g.Count(),
        TotalSize = g.Sum(f => (long?)f.FileSizeBytes) ?? 0L
      })
      .ToDictionaryAsync(x => x.FolderId, x => new { x.Count, x.TotalSize }, cancellationToken);

    var folderDtos = folders.Select(f => new FolderContentDto(
      f.Id.Value,
      f.Name,
      f.Description,
      f.BlobPrefix,
      subfolderCounts.GetValueOrDefault(f.Id, 0),
      fileStats.GetValueOrDefault(f.Id)?.Count ?? 0,
      fileStats.GetValueOrDefault(f.Id)?.TotalSize ?? 0L,
      f.CreatedOnUtc,
      f.ModifiedOnUtc
    )).ToArray();

    // Get files (if there's room in pagination)
    var remainingPageSize = pageSize - folders.Count;
    var fileSkip = Math.Max(0, (page - 1) * pageSize - folderCount);

    var files = remainingPageSize > 0 && fileSkip >= 0
      ? await filesQuery
          .GroupJoin(
            db.FileMetadata,
            f => f.Id,
            m => m.FileId,
            (f, m) => new { File = f, Metadata = m.FirstOrDefault() })
          .Select(fm => new FileContentDto(
            fm.File.Id.Value,
            EF.Property<string>(fm.File, "OriginalFileName"),
            fm.File.Extension,
            fm.File.ContentType,
            fm.File.FileSizeBytes,
            fm.File.BlobUrl,
            fm.Metadata != null ? fm.Metadata.Status.Name : "Pending",
            fm.File.CreatedOnUtc
          ))
          .Skip(fileSkip)
          .Take(remainingPageSize)
          .AsNoTracking()
          .ToListAsync(cancellationToken)
      : [];

    // Calculate stats
    var totalFolders = await db.Folders
      .Where(f => folderId.HasValue
        ? f.ParentFolderId == folderId.Value
        : f.StorageId == effectiveStorageId && f.ParentFolderId == null)
      .CountAsync(cancellationToken);

    var totalFiles = await db.Files
      .Where(f => folderId.HasValue
        ? f.FolderId == folderId.Value
        : db.Folders.Where(folder => folder.StorageId == effectiveStorageId && folder.ParentFolderId == null)
            .Select(folder => folder.Id)
            .Contains(f.FolderId))
      .CountAsync(cancellationToken);

    var totalSizeBytes = await db.Files
      .Where(f => folderId.HasValue
        ? f.FolderId == folderId.Value
        : db.Folders.Where(folder => folder.StorageId == effectiveStorageId && folder.ParentFolderId == null)
            .Select(folder => folder.Id)
            .Contains(f.FolderId))
      .SumAsync(f => (long?)f.FileSizeBytes, cancellationToken) ?? 0L;

    var stats = new FolderStats(totalFolders, totalFiles, totalSizeBytes);

    var hasNextPage = page * pageSize < totalCount;
    var hasPreviousPage = page > 1;

    return new GetFolderContentsResult(
      breadcrumbs,
      folderDtos,
      files.ToArray(),
      stats,
      totalCount,
      page,
      pageSize,
      hasNextPage,
      hasPreviousPage
    );
  }

  private async Task<FolderBreadcrumb[]> BuildBreadcrumbsAsync(
    FolderId? folderId,
    StorageId storageId,
    CancellationToken cancellationToken)
  {
    var breadcrumbs = new List<FolderBreadcrumb>();

    // Add root breadcrumb
    var storage = await db.Storages
      .Where(s => s.Id == storageId)
      .Select(s => new { Name = EF.Property<string>(s, "StorageName") })
      .FirstOrDefaultAsync(cancellationToken);

    breadcrumbs.Add(new FolderBreadcrumb(null, storage?.Name ?? "Storage", "/"));

    // If a folder is specified, build the path
    if (folderId.HasValue)
    {
      var path = new List<(int Id, string Name)>();
      FolderId? currentFolderId = folderId.Value;

      while (currentFolderId.HasValue)
      {
        var folder = await db.Folders
          .Where(f => f.Id == currentFolderId.Value)
          .Select(f => new { f.Id, Name = EF.Property<string>(f, "Name"), f.ParentFolderId })
          .FirstOrDefaultAsync(cancellationToken);

        if (folder == null) break;

        path.Insert(0, (folder.Id.Value, folder.Name));
        currentFolderId = folder.ParentFolderId;
      }

      // Build breadcrumb path
      var currentPath = "/";
      foreach (var (id, name) in path)
      {
        currentPath += name + "/";
        breadcrumbs.Add(new FolderBreadcrumb(id, name, currentPath));
      }
    }

    return breadcrumbs.ToArray();
  }

  private static IQueryable<Folder> ApplyFolderSorting(
    IQueryable<Folder> query,
    string sortBy,
    string sortOrder)
  {
    var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

    return sortBy.ToLower() switch
    {
      "date" => isAscending
        ? query.OrderBy(f => f.CreatedOnUtc)
        : query.OrderByDescending(f => f.CreatedOnUtc),
      "name" or _ => isAscending
        ? query.OrderBy(f => EF.Property<string>(f, "Name"))
        : query.OrderByDescending(f => EF.Property<string>(f, "Name"))
    };
  }

  private static IQueryable<File> ApplyFileSorting(
    IQueryable<File> query,
    string sortBy,
    string sortOrder)
  {
    var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

    return sortBy.ToLower() switch
    {
      "date" => isAscending
        ? query.OrderBy(f => f.CreatedOnUtc)
        : query.OrderByDescending(f => f.CreatedOnUtc),
      "size" => isAscending
        ? query.OrderBy(f => f.FileSizeBytes)
        : query.OrderByDescending(f => f.FileSizeBytes),
      "name" or _ => isAscending
        ? query.OrderBy(f => EF.Property<string>(f, "OriginalFileName"))
        : query.OrderByDescending(f => EF.Property<string>(f, "OriginalFileName"))
    };
  }
}
