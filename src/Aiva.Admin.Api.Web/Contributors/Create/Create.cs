using Aiva.Admin.Api.Core.ContributorAggregate;
using Aiva.Admin.Api.UseCases.Contributors.Create;
using Aiva.Admin.Api.Web.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Contributors.Create;

public class Create(IMediator mediator)
  : Endpoint<CreateContributorRequest,
      Results<Created<CreateContributorResponse>,
              ValidationProblem,
              ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(CreateContributorRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Create a new contributor";
      s.Description = "Creates a new contributor with the provided name. The contributor name must be between 2 and 100 characters long.";
      s.ExampleRequest = new CreateContributorRequest { Name = "John Doe" };
      s.ResponseExamples[201] = new CreateContributorResponse(1, "John Doe");

      s.Responses[201] = "Contributor created successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[500] = "Internal server error";
    });

    Tags("Contributors");

    Description(builder => builder
      .Accepts<CreateContributorRequest>("application/json")
      .Produces<CreateContributorResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(500));
  }

  public override async Task<Results<Created<CreateContributorResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateContributorRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateContributorCommand(ContributorName.From(request.Name!), request.PhoneNumber);
    var result = await _mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      id => $"/Contributors/{id}",
      id => new CreateContributorResponse(id.Value, request.Name!));
  }
}
