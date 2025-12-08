using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Files.Upload;

using Core.FileAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;
using Extensions;
using UseCases.Files.Upload;

public class Upload(IMediator mediator)
    : Endpoint<UploadFileRequest,
        Results<Created<UploadFileResponse>,
                ValidationProblem,
                ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(UploadFileRequest.Route);
    AllowFileUploads();
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Upload a file to storage";
      s.Description = $"Uploads a file to the specified storage and folder. " +
                    $"Allowed extensions: {string.Join(", ", AllowedFileExtensions.Extensions)}. " +
                    $"Max file size: 50 MB.";

      s.Responses[201] = "File uploaded successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[404] = "Storage or Folder not found";
      s.Responses[500] = "Internal server error";
    });

    Tags("Files");

    Description(builder => builder
        .Accepts<UploadFileRequest>("multipart/form-data")
        .Produces<UploadFileResponse>(201, "application/json")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500));
  }

  public override async Task<Results<Created<UploadFileResponse>, ValidationProblem, ProblemHttpResult>>
      ExecuteAsync(UploadFileRequest request, CancellationToken cancellationToken)
  {
    var file = request.File!;
    var extension = Path.GetExtension(file.FileName);
    var contentType = AllowedFileExtensions.GetContentType(extension);

    await using var stream = file.OpenReadStream();

    var command = new UploadFileCommand(
        StorageId.From(request.StorageId),
        FolderId.From(request.FolderId),
        file.FileName,
        extension,
        contentType,
        file.Length,
        stream);

    var result = await _mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
        dto => $"/files/{dto.Id}",
        dto => new UploadFileResponse(
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
            dto.CreatedOnUtc));
  }
}
