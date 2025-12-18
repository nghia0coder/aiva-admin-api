using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Folders.Create;

using Core.FolderAggregate;
using Core.StorageAggregate;
using Extensions;
using UseCases.Folders.Create;

public class Create(IMediator mediator)
  : Endpoint<CreateFolderRequest,
      Results<Created<CreateFolderResponse>,
              ValidationProblem,
              ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(CreateFolderRequest.Route);
    Summary(s =>
    {
      s.Summary = "Create a new folder";
      s.Description = "Creates a new folder within a storage. Optionally can be nested under a parent folder.";
      s.ExampleRequest = new CreateFolderRequest
      {
        FolderName = "My Documents",
        StorageId = 1,
        ParentFolderId = null
      };
      s.ResponseExamples[201] = new CreateFolderResponse(1, "My Documents", 1, null);

      s.Responses[201] = "Folder created successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[500] = "Internal server error";
    });

    Tags("Folders");

    Description(builder => builder
      .Accepts<CreateFolderRequest>("application/json")
      .Produces<CreateFolderResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(500));
  }

  public override async Task<Results<Created<CreateFolderResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateFolderRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateFolderCommand(
      StorageId.From(request.StorageId),
      FolderName.From(request.FolderName),
      request.ParentFolderId.HasValue ? FolderId.From(request.ParentFolderId.Value) : null
    );

    var result = await _mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      id => $"/Folders/{id}",
      id => new CreateFolderResponse(
        id.Value,
        request.FolderName,
        request.StorageId,
        request.ParentFolderId));
  }
}
