namespace Aiva.Admin.Api.Web.Files.Upload;

public record UploadResponse(
    IReadOnlyList<UploadFileResponse> UploadedFiles,
    int TotalFiles,
    int SuccessfulUploads,
    int FailedUploads)
{
  /// <summary>
  /// For single file uploads, return just the first (and only) file
  /// </summary>
  public UploadFileResponse? SingleFile => UploadedFiles.FirstOrDefault();
  
  /// <summary>
  /// Indicates if this was a single file upload
  /// </summary>
  public bool IsSingleFileUpload => TotalFiles == 1;
}
