namespace Aiva.Admin.Api.Web.Files.GetStatus;

public class GetStatusRequest
{
  public const string Route = "/files/{FileId:int}/status";

  public static string BuildRoute(int fileId) => Route.Replace("{FileId:int}", fileId.ToString());

  public int FileId { get; set; }
}
