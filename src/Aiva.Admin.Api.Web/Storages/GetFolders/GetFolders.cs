namespace Aiva.Admin.Api.Web.Storages.GetFolders;

using Aiva.Admin.Api.UseCases.Folders;
using Core.StorageAggregate;
using UseCases.Folders.GetByStorage;

public sealed class GetFolders(IMediator mediator) : Endpoint<GetFoldersRequest, GetFoldersResponse>
{
  public override void Configure()
  {
    Get("/storages/{StorageId}/folders");
    Summary(s =>
    {
      s.Summary = "Get all folders for a storage";
      s.Description = "Returns all folders including nested subfolders for the specified storage. Can return as tree or flat list.";
    });
  }

  public override async Task HandleAsync(GetFoldersRequest request, CancellationToken ct)
  {
    var query = new GetFoldersByStorageQuery(
      StorageId.From(request.StorageId),
      request.AsTree);

    var result = await mediator.Send(query, ct);

    if (result.IsNotFound())
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var response = new GetFoldersResponse(
      request.StorageId,
      result.Value.Tree?.Select(MapToRecord).ToList(),
      result.Value.FlatList?.Select(MapToFlatRecord).ToList());

    await Send.OkAsync(response, ct);
  }

  private static FolderTreeRecord MapToRecord(FolderTreeNodeDto dto) =>
    new(dto.Id.Value, dto.Name.Value, dto.Description, dto.BlobPrefix,
        dto.Children.Select(MapToRecord).ToList());

  private static FolderFlatRecord MapToFlatRecord(FolderDto dto) =>
    new(dto.Id.Value, dto.Name.Value, dto.Description, dto.BlobPrefix,
        dto.ParentFolderId?.Value, dto.CreatedOnUtc);
}
