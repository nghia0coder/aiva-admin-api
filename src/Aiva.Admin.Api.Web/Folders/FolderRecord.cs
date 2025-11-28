namespace Aiva.Admin.Api.Web.Folders;

public record FolderRecord(
  int Id,
  string FolderName,
  string? Description,
  string BlobPrefix,
  int StorageId,
  int? ParentFolderId,
  DateTime CreatedOnUtc
);
