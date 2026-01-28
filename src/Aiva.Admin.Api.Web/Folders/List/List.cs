namespace Aiva.Admin.Api.Web.Folders.List;

using Aiva.Admin.Api.UseCases.Folders;
using Aiva.Admin.Api.UseCases.Folders.List;
using Aiva.Admin.Api.Web.Common;
using Core.StorageAggregate;

public sealed class List(IMediator mediator) : AuthenticatedEndpoint<ListFoldersRequest, ListFoldersResponse>
{
  public override void Configure()
  {
    Get("/folders");
    Summary(s =>
    {
      s.Summary = "Get list of folders";
      s.Description = "Returns a paginated list of folders with optional filtering by storage, parent folder, and search term.";
    });
  }

  public override async Task HandleAsync(ListFoldersRequest request, CancellationToken ct)
  {
    var query = new ListFoldersQuery(
      StorageId: request.StorageId.HasValue ? StorageId.From(request.StorageId.Value) : null,
      ParentFolderId: request.ParentFolderId,
      Page: request.Page,
      PageSize: request.PageSize,
      SearchTerm: request.SearchTerm,
      IncludeChildren: request.IncludeChildren);

    var result = await mediator.Send(query, ct);

    if (result.IsNotFound())
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (result.IsInvalid())
    {
      await Send.NoContentAsync(ct);
      return;
    }

    var response = new ListFoldersResponse(
      Folders: result.Value.Folders.Select(MapToRecord).ToList(),
      TotalCount: result.Value.TotalCount,
      Page: result.Value.Page,
      PageSize: result.Value.PageSize,
      HasNextPage: result.Value.HasNextPage,
      HasPreviousPage: result.Value.HasPreviousPage);

    await Send.OkAsync(response, ct);
  }

  private static FolderRecord MapToRecord(FolderDto dto) =>
    new(
      Id: dto.Id.Value,
      FolderName: dto.Name.Value,
      Description: dto.Description,
      BlobPrefix: dto.BlobPrefix,
      StorageId: dto.StorageId.Value,
      ParentFolderId: dto.ParentFolderId?.Value,
      CreatedOnUtc: dto.CreatedOnUtc);
}
