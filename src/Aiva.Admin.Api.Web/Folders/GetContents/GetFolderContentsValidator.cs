using FluentValidation;

namespace Aiva.Admin.Api.Web.Folders.GetContents;

public class GetFolderContentsValidator : Validator<GetFolderContentsRequest>
{
  public GetFolderContentsValidator()
  {
    // Either FolderId or StorageId must be provided
    RuleFor(x => x)
      .Must(x => x.FolderId.HasValue || x.StorageId.HasValue)
      .WithMessage("Either FolderId or StorageId must be provided");

    RuleFor(x => x.Page)
      .GreaterThan(0)
      .WithMessage("Page must be greater than 0");

    RuleFor(x => x.PageSize)
      .InclusiveBetween(1, 100)
      .WithMessage("PageSize must be between 1 and 100");

    RuleFor(x => x.SortBy)
      .Must(x => x == null || new[] { "name", "date", "size" }.Contains(x.ToLower()))
      .WithMessage("SortBy must be one of: name, date, size");

    RuleFor(x => x.SortOrder)
      .Must(x => x == null || new[] { "asc", "desc" }.Contains(x.ToLower()))
      .WithMessage("SortOrder must be one of: asc, desc");
  }
}
