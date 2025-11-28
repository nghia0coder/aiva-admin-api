namespace Aiva.Admin.Api.Web.Folders.Create;

public class CreateFolderResponse(
  int Id,
  string FolderName,
  int StorageId,
  int? ParentFolderId
)
{
  public int Id { get; } = Id;
  public string FolderName { get; set; } = FolderName;
  public int StorageId { get; set; } = StorageId;
  public int? ParentFolderId { get; set; } = ParentFolderId;
}
