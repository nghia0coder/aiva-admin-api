namespace Aiva.Admin.Api.UseCases.Folders.GetContents;

using Core.FolderAggregate;
using Core.StorageAggregate;

public record GetFolderContentsQuery(
  FolderId? FolderId,
  StorageId? StorageId,
  string? SearchQuery,
  string SortBy,
  string SortOrder,
  int Page,
  int PageSize
) : IQuery<Result<GetFolderContentsResult>>;
