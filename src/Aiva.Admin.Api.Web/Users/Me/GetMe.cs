using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Users.Me;

using Core.Interfaces;
using UseCases.Users;
using UseCases.Users.SyncCurrentUser;

public class GetMe : EndpointWithoutRequest<Results<Ok<UserDto>, UnauthorizedHttpResult>>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUser;

  public GetMe(IMediator mediator, ICurrentUserService currentUser)
  {
    _mediator = mediator;
    _currentUser = currentUser;
  }

  public override void Configure()
  {
    Get("/users/me");
    Summary(s =>
    {
      s.Summary = "Get or create current user profile";
      s.Description = "Returns the current user's profile, creating it if first login.";
    });
    Tags("Users");
  }

  public override async Task<Results<Ok<UserDto>, UnauthorizedHttpResult>> ExecuteAsync(CancellationToken ct)
  {
    if (!_currentUser.IsAuthenticated || _currentUser.AzureAdObjectId is null)
    {
      return TypedResults.Unauthorized();
    }

    var command = new SyncCurrentUserCommand(
        _currentUser.AzureAdObjectId,
        _currentUser.Email ?? "",
        _currentUser.DisplayName ?? "Unknown User",
        null, // Extract from claims if needed
        null);

    var result = await _mediator.Send(command, ct);

    return TypedResults.Ok(result.Value);
  }
}
