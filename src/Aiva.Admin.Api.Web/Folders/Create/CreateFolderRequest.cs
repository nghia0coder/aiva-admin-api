namespace Aiva.Admin.Api.Web.Folders.Create;

public class CreateFolderRequest
{
  public const string Route = "/Folder";
  public string FolderName { get; set; } = null!;
  public int StorageId { get; set; }
  public int? ParentFolderId { get; set; }
}
