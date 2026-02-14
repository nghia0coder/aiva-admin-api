namespace Aiva.Admin.Api.Web.Files.Upload;

public class UploadFilesRequest
{
  public const string Route = "/files/upload";

  public int StorageId { get; set; }
  public int FolderId { get; set; }
  public IFormFileCollection? Files { get; set; }
}
