namespace Aiva.Admin.Api.UseCases.Folders.GetByStorage;

using Core.StorageAggregate;

public record GetFoldersByStorageQuery(StorageId StorageId, bool AsTree = true) : IQuery<Result<FoldersByStorageResult>>;

public record FoldersByStorageResult(
  StorageId StorageId,
  IReadOnlyList<FolderTreeNodeDto>? Tree,
  IReadOnlyList<FolderDto>? FlatList
);
