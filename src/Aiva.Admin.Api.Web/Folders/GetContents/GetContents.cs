namespace Aiva.Admin.Api.Web.Folders.GetContents;

using Aiva.Admin.Api.UseCases.Folders.GetContents;
using Aiva.Admin.Api.Web.Common;
using Core.FolderAggregate;
using Core.StorageAggregate;

public sealed class GetContents(IMediator mediator)
  : AuthenticatedEndpoint<GetFolderContentsRequest, GetFolderContentsResponse>
{
  public override void Configure()
  {
    Get("/folders/contents");
    Summary(s =>
    {
      s.Summary = "Get folder contents";
      s.Description = "Returns folders and files for the specified folder or storage root. " +
                     "Includes breadcrumbs for navigation and statistics. Supports search, sorting, and pagination.";
      s.ExampleRequest = new GetFolderContentsRequest
      {
        FolderId = 123,
        SortBy = "name",
        SortOrder = "asc",
        Page = 1,
        PageSize = 50
      };
    });
  }

  public override async Task HandleAsync(GetFolderContentsRequest request, CancellationToken ct)
  {
    var query = new GetFolderContentsQuery(
      FolderId: request.FolderId.HasValue ? FolderId.From(request.FolderId.Value) : null,
      StorageId: request.StorageId.HasValue ? StorageId.From(request.StorageId.Value) : null,
      SearchQuery: request.SearchQuery,
      SortBy: request.SortBy ?? "name",
      SortOrder: request.SortOrder ?? "asc",
      Page: request.Page,
      PageSize: request.PageSize
    );

    var result = await mediator.Send(query, ct);

    if (result.IsNotFound())
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (result.IsInvalid())
    {
      AddError("Invalid request parameters");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    var value = result.Value;

    var response = new GetFolderContentsResponse(
      Breadcrumbs: value.Breadcrumbs.Select(MapToBreadcrumbRecord).ToArray(),
      Folders: value.Folders.Select(MapToFolderCardRecord).ToArray(),
      Files: value.Files.Select(MapToFileCardRecord).ToArray(),
      Stats: MapToStatsRecord(value.Stats),
      Pagination: new PaginationRecord(
        value.Page,
        value.PageSize,
        value.TotalCount,
        value.HasNextPage,
        value.HasPreviousPage
      )
    );

    await Send.OkAsync(response, ct);
  }

  private static FolderBreadcrumbRecord MapToBreadcrumbRecord(FolderBreadcrumb breadcrumb) =>
    new(breadcrumb.Id, breadcrumb.Name, breadcrumb.Path);

  private static FolderCardRecord MapToFolderCardRecord(FolderContentDto dto) =>
    new(
      dto.Id,
      dto.Name,
      dto.Description,
      dto.BlobPrefix,
      dto.SubfolderCount,
      dto.FileCount,
      dto.TotalSizeBytes,
      dto.CreatedOnUtc,
      dto.UpdatedOnUtc
    );

  private static FileCardRecord MapToFileCardRecord(FileContentDto dto) =>
    new(
      dto.Id,
      dto.OriginalFileName,
      dto.Extension,
      dto.ContentType,
      dto.FileSizeBytes,
      dto.BlobUrl,
      dto.ProcessingStatus,
      dto.CreatedOnUtc
    );

  private static FolderStatsRecord MapToStatsRecord(FolderStats stats) =>
    new(stats.TotalFolders, stats.TotalFiles, stats.TotalSizeBytes);
}
