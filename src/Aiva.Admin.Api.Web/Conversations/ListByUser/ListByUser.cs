using Aiva.Admin.Api.Core.UserAggregate;
using Aiva.Admin.Api.UseCases.Conversations.ListByUser;

namespace Aiva.Admin.Api.Web.Conversations.ListByUser;

public class ListByUser(IMediator mediator)
    : Endpoint<ListUserConversationsRequest, ListUserConversationsResponse, ListUserConversationsMapper>
{
  public override void Configure()
  {
    Get(ListUserConversationsRequest.Route);
    Summary(s =>
    {
      s.Summary = "Get user's conversation history";
      s.Description = "Retrieves a paginated list of all conversations for a specific user, " +
                      "ordered by most recent activity.";
      s.ExampleRequest = new ListUserConversationsRequest
      {
        UserId = 1,
        Page = 1,
        PerPage = 10
      };
      s.Params["UserId"] = "The ID of the user";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{UseCases.Constants.MAX_PAGE_SIZE}";
      s.Responses[200] = "Paginated conversation list returned successfully";
      s.Responses[400] = "Invalid request parameters";
      s.Responses[404] = "User not found";
    });
    Tags("Conversations");
  }

  public override async Task HandleAsync(
      ListUserConversationsRequest request,
      CancellationToken ct)
  {
    var query = new ListUserConversationsQuery(
        UserId.From(request.UserId),
        request.Page,
        request.PerPage);

    var result = await mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(request.UserId, pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    var response = Map.FromEntity(pagedResult);
    await Send.OkAsync(response, ct);
  }

  private void AddLinkHeader(int userId, int page, int perPage, int totalPages)
  {
    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/users/{userId}/conversations";
    string Link(string rel, int p) => $"<{baseUrl}?page={p}&per_page={perPage}>; rel=\"{rel}\"";

    var parts = new List<string>();
    if (page > 1)
    {
      parts.Add(Link("first", 1));
      parts.Add(Link("prev", page - 1));
    }
    if (page < totalPages)
    {
      parts.Add(Link("next", page + 1));
      parts.Add(Link("last", totalPages));
    }

    if (parts.Count > 0)
      HttpContext.Response.Headers["Link"] = string.Join(", ", parts);
  }
}
