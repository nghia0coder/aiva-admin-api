namespace Aiva.Admin.Api.Web.Conversations.ListMine;

using UseCases.Conversations.ListByUser;
using Common;

public class ListMine(IMediator mediator)
    : AuthenticatedEndpoint<ListMyConversationsRequest, ListMyConversationsResponse>
{
  public override void Configure()
  {
    Get(ListMyConversationsRequest.Route);
    Summary(s =>
    {
      s.Summary = "Get my conversation history";
      s.Description = "Retrieves a paginated list of all conversations for the authenticated user, " +
                      "ordered by most recent activity.";
      s.ExampleRequest = new ListMyConversationsRequest
      {
        Page = 1,
        PerPage = 10
      };
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{UseCases.Constants.MAX_PAGE_SIZE}";
      s.Responses[200] = "Paginated conversation list returned successfully";
      s.Responses[400] = "Invalid request parameters";
      s.Responses[401] = "User not authenticated";
    });
    Tags("Conversations");
  }

  public override async Task HandleAsync(
      ListMyConversationsRequest request,
      CancellationToken ct)
  {
    if (!IsAuthenticated)
    {
      await Send.UnauthorizedAsync(ct);
      return;
    }

    var query = new ListUserConversationsQuery(
        RequiredUserId,
        request.Page,
        request.PerPage);

    var result = await mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    // Manual mapping instead of using TMapper
    var response = new ListMyConversationsResponse(
        pagedResult.Items.Select(c => new ConversationRecord(
            c.Id,
            c.Title,
            c.CreatedAt,
            c.LastMessageAt,
            c.MessageCount)).ToList(),
        pagedResult.Page,
        pagedResult.PerPage,
        pagedResult.TotalCount,
        pagedResult.TotalPages);

    await Send.OkAsync(response, ct);
  }

  private void AddLinkHeader(int page, int perPage, int totalPages)
  {
    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/conversations";
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
