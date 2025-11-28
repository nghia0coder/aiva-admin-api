namespace Aiva.Admin.Api.Web.Storages.GetFolders;

public record GetFoldersResponse(
  int StorageId,
  IReadOnlyList<FolderTreeRecord>? Tree,
  IReadOnlyList<FolderFlatRecord>? FlatList);

public record FolderTreeRecord(
  int Id,
  string Name,
  string? Description,
  string BlobPrefix,
  IReadOnlyList<FolderTreeRecord> Children);

public record FolderFlatRecord(
  int Id,
  string Name,
  string? Description,
  string BlobPrefix,
  int? ParentFolderId,
  DateTime CreatedOnUtc);
