namespace Aiva.Admin.Api.Web.Files.Upload;

public class UploadFileRequest
{
  public const string Route = "/files/upload";

  public int StorageId { get; set; }
  public int FolderId { get; set; }
  public IFormFile? File { get; set; }
}
