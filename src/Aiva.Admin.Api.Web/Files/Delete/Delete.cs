using Aiva.Admin.Api.Core.FileAggregate;
using Aiva.Admin.Api.UseCases.Files.Delete;

namespace Aiva.Admin.Api.Web.Files.Delete;

/// <summary>
/// Endpoint to delete a file from blob storage, Azure AI Search, and database
/// </summary>
public sealed class Delete(IMediator mediator)
  : Endpoint<DeleteFileRequest>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Delete(DeleteFileRequest.Route);

    Summary(s =>
    {
      s.Summary = "Delete a file";
      s.Description = "Deletes a file from blob storage, Azure AI Search index, and database. " +
                      "This operation removes the file completely from all systems.";
      s.Responses[204] = "File deleted successfully";
      s.Responses[404] = "File not found";
      s.Responses[400] = "Bad request or partial deletion failure";
    });

    Tags("Files");
  }

  public override async Task HandleAsync(
      DeleteFileRequest request,
      CancellationToken cancellationToken)
  {
    var command = new DeleteFileCommand(FileId.From(request.Id));
    var result = await _mediator.Send(command, cancellationToken);

    if (!result.IsSuccess)
    {
      if (result.Status == ResultStatus.NotFound)
      {
        await Send.NotFoundAsync(cancellationToken);
        return;
      }

      await Send.ErrorsAsync(
          400,
          cancellation: cancellationToken);
      return;
    }

    await Send.NoContentAsync(cancellationToken);
  }
}
