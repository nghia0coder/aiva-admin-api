using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Files.Upload;

using Core.FileAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;
using Extensions;
using UseCases.Files.Upload;

public class Upload(IMediator mediator)
    : Endpoint<UploadFilesRequest,
        Results<Created<UploadResponse>,
                ValidationProblem,
                ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(UploadFilesRequest.Route);
    AllowFileUploads();
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Upload one or multiple files to storage";
      s.Description = $"Uploads one or multiple files to the specified storage and folder. " +
                    $"Can handle single file or multiple files in the same request. " +
                    $"Allowed extensions: {string.Join(", ", AllowedFileExtensions.Extensions)}. " +
                    $"Max file size: 50 MB per file.";

      s.Responses[201] = "File(s) uploaded successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[404] = "Storage or Folder not found";
      s.Responses[500] = "Internal server error";
    });

    Tags("Files");

    Description(builder => builder
        .Accepts<UploadFilesRequest>("multipart/form-data")
        .Produces<UploadResponse>(201, "application/json")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500));
  }

  public override async Task<Results<Created<UploadResponse>, ValidationProblem, ProblemHttpResult>>
      ExecuteAsync(UploadFilesRequest request, CancellationToken cancellationToken)
  {
    if (request.Files is null || request.Files.Count == 0)
    {
      return TypedResults.ValidationProblem(new Dictionary<string, string[]>
      {
        ["Files"] = ["No files provided for upload."]
      });
    }

    var fileInfos = new List<FileUploadInfo>();

    // Process each file and create FileUploadInfo objects
    foreach (var file in request.Files)
    {
      var extension = Path.GetExtension(file.FileName);
      var contentType = AllowedFileExtensions.GetContentType(extension);

      // Create a memory stream to avoid stream disposal issues
      var memoryStream = new MemoryStream();
      await using (var fileStream = file.OpenReadStream())
      {
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
      }
      memoryStream.Position = 0;

      fileInfos.Add(new FileUploadInfo(
          file.FileName,
          extension,
          contentType,
          file.Length,
          memoryStream));
    }

    var command = new UploadMultipleFilesCommand(
        StorageId.From(request.StorageId),
        FolderId.From(request.FolderId),
        fileInfos.AsReadOnly());

    var result = await _mediator.Send(command, cancellationToken);

    // Dispose memory streams
    foreach (var fileInfo in fileInfos)
    {
      fileInfo.FileContent.Dispose();
    }

    if (!result.IsSuccess)
    {
      return result.ToProblemResult();
    }

    var uploadedFiles = result.Value.Select(dto => new UploadFileResponse(
        dto.Id,
        dto.OriginalFileName,
        dto.StoredFileName,
        dto.Extension,
        dto.ContentType,
        dto.FileSizeBytes,
        dto.BlobPath,
        dto.BlobUrl,
        dto.StorageId,
        dto.FolderId,
        dto.FileProcessingStatus,
        dto.QueuedAt,
        dto.CreatedOnUtc)).ToList();

    var response = new UploadResponse(
        uploadedFiles.AsReadOnly(),
        request.Files.Count,
        uploadedFiles.Count,
        request.Files.Count - uploadedFiles.Count);

    // For single file uploads, create location pointing to the single file
    var location = response.IsSingleFileUpload 
      ? $"/files/{response.SingleFile!.Id}"
      : "/files/upload";

    return TypedResults.Created(location, response);
  }
}
