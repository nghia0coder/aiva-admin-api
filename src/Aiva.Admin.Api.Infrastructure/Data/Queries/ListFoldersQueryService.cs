namespace Aiva.Admin.Api.Infrastructure.Data.Queries;

using Core.FolderAggregate;
using Core.StorageAggregate;
using Microsoft.EntityFrameworkCore;
using UseCases.Folders;
using UseCases.Folders.List;

public class ListFoldersQueryService(AppDbContext db) : IListFoldersQueryService
{
  public async Task<ListFoldersResult> GetFoldersAsync(
    StorageId? storageId,
    int? parentFolderId,
    int page,
    int pageSize,
    string? searchTerm = null,
    bool includeChildren = false,
    CancellationToken cancellationToken = default)
  {
    var query = db.Folders.AsQueryable();

    // Apply filters
    if (storageId.HasValue)
    {
      query = query.Where(f => f.StorageId == storageId.Value);
    }

    if (parentFolderId.HasValue)
    {
      if (includeChildren)
      {
        // Get the parent folder and all its descendants
        var parentFolder = await db.Folders
          .Where(f => f.Id == FolderId.From(parentFolderId.Value))
          .Select(f => f.BlobPrefix)
          .FirstOrDefaultAsync(cancellationToken);

        if (parentFolder != null)
        {
          // Include parent folder and all folders whose BlobPrefix starts with parent's BlobPrefix
          query = query.Where(f => f.BlobPrefix.StartsWith(parentFolder));
        }
        else
        {
          // If parent folder not found, return empty result
          return new ListFoldersResult([], 0, page, pageSize, false, false);
        }
      }
      else
      {
        // Only direct children
        query = query.Where(f => f.ParentFolderId == FolderId.From(parentFolderId.Value));
      }
    }
    else
    {
      // When no parentFolderId is specified, only return root level folders (folders without a parent)
      query = query.Where(f => f.ParentFolderId == null);
    }

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var lowerSearchTerm = searchTerm.ToLower();
      query = query.Where(f => 
        EF.Property<string>(f, "Name").ToLower().Contains(lowerSearchTerm) ||
        (f.Description != null && f.Description.ToLower().Contains(lowerSearchTerm)));
    }

    // Get total count before pagination
    var totalCount = await query.CountAsync(cancellationToken);

    // Apply pagination and ordering
    var folders = await query
      .OrderBy(f => f.BlobPrefix) // Natural hierarchical ordering
      .ThenBy(f => EF.Property<string>(f, "Name"))
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(f => new FolderDto(
        f.Id,
        f.Name,
        f.Description,
        f.BlobPrefix,
        f.StorageId,
        f.ParentFolderId,
        f.CreatedOnUtc))
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    var hasNextPage = page * pageSize < totalCount;
    var hasPreviousPage = page > 1;

    return new ListFoldersResult(
      folders,
      totalCount,
      page,
      pageSize,
      hasNextPage,
      hasPreviousPage);
  }
}
