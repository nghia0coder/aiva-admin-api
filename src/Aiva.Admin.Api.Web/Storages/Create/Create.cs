using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Storages.Create;

using Core.StorageAggregate;
using Extensions;
using UseCases.Storages.Create;

public class Create(IMediator mediator)
  : Endpoint<CreateStorageRequest,
      Results<Created<CreateStorageResponse>,
              ValidationProblem,
              ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(CreateStorageRequest.Route);
    Summary(s =>
    {
      s.Summary = "Create a new storage";
      s.Description = "Creates a new storage with the provided name. The storage name must be between 2 and 100 characters long.";
      s.ExampleRequest = new CreateStorageRequest { StorageName = "John Doe" };
      s.ResponseExamples[201] = new CreateStorageResponse(1, "Sample Storage Name", "Sample storage description");

      s.Responses[201] = "Storage created successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[500] = "Internal server error";
    });

    Tags("Storages");

    Description(builder => builder
      .Accepts<CreateStorageRequest>("application/json")
      .Produces<CreateStorageResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(500));
  }

  public override async Task<Results<Created<CreateStorageResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateStorageRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateStorageCommand(StorageName.From(request.StorageName!), request.StorageDescription);
    var result = await _mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      id => $"/Storages/{id}",
      id => new CreateStorageResponse(id.Value, request.StorageName, request.StorageDescription));
  }
}
