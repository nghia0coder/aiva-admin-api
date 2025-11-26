namespace Aiva.Admin.Api.Web.Storages.List;

using UseCases.Storages.List;

public class List(IMediator mediator)
  : Endpoint<ListStoragesRequest, ListStorageResponse, ListStoragesMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/Storages");
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "List storages with pagination";
      s.Description = "Retrieves a paginated list of all storages. Supports GitHub-style pagination with 1-based page indexing and configurable page size.";
      s.ExampleRequest = new ListStoragesRequest { Page = 1, PerPage = 10 };
      s.ResponseExamples[200] = new ListStorageResponse(
        new List<StorageRecord>
        {
          new(1, "Storage 1", "Sample Storage Description 1"),
          new(2, "Storage 2", "Sample Storage Description 2")
        },
        1, 10, 2, 1);

      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{UseCases.Constants.MAX_PAGE_SIZE} (default {UseCases.Constants.DEFAULT_PAGE_SIZE})";
      s.Responses[200] = "Paginated list of storages returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Storages");

    Description(builder => builder
      .Accepts<ListStoragesRequest>()
      .Produces<ListStorageResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListStoragesRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListStoragesQuery(request.Page, request.PerPage), cancellationToken);
    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    var response = Map.FromEntity(pagedResult);
    await Send.OkAsync(response, cancellationToken);
  }

  private void AddLinkHeader(int page, int perPage, int totalPages)
  {
    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
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

