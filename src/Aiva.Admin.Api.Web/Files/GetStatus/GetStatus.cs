using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Files.GetStatus;

using Core.FileAggregate;
using Extensions;
using UseCases.Files.GetStatus;

public class GetStatus(IMediator mediator)
    : Endpoint<GetStatusRequest,
        Results<Ok<GetStatusResponse>,
                NotFound,
                ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetStatusRequest.Route);
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Get file processing status";
      s.Description = "Returns the current processing status and metadata for a file. " +
                    "Use this endpoint to check if file processing has completed.";
      s.ExampleRequest = new GetStatusRequest { FileId = 1 };

      s.Responses[200] = "File status retrieved successfully";
      s.Responses[404] = "File or FileMetadata not found";
    });

    Tags("Files");

    Description(builder => builder
        .Accepts<GetStatusRequest>()
        .Produces<GetStatusResponse>(200, "application/json")
        .ProducesProblem(404));
  }

  public override async Task<Results<Ok<GetStatusResponse>, NotFound, ProblemHttpResult>>
      ExecuteAsync(GetStatusRequest request, CancellationToken ct)
  {
    var query = new GetFileStatusQuery(FileId.From(request.FileId));
    var result = await mediator.Send(query, ct);

    return result.ToGetByIdResult(dto => new GetStatusResponse(
        dto.FileId,
        dto.MetadataId,
        dto.Status,
        dto.QueuedAt,
        dto.ProcessingStartedAt,
        dto.ProcessingCompletedAt,
        dto.RetryCount,
        dto.ErrorMessage,
        new ProcessingMetadataInfo(
            dto.PageCount,
            dto.WordCount,
            dto.DetectedLanguage,
            dto.IsEmbedded,
            dto.EmbeddedAt)));
  }
}
