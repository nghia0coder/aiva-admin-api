namespace Aiva.Admin.Api.Web.Files.Delete;

/// <summary>
/// Request to delete a file
/// </summary>
public sealed record DeleteFileRequest
{
  public const string Route = "/files/{Id}";
  
  /// <summary>
  /// The ID of the file to delete
  /// </summary>
  public int Id { get; init; }
}
