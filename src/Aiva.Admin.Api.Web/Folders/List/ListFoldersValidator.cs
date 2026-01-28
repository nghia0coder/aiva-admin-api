namespace Aiva.Admin.Api.Web.Folders.List;

using FluentValidation;

public class ListFoldersValidator : Validator<ListFoldersRequest>
{
  public ListFoldersValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThan(0)
      .WithMessage("Page must be greater than 0");

    RuleFor(x => x.PageSize)
      .InclusiveBetween(1, 100)
      .WithMessage("Page size must be between 1 and 100");

    RuleFor(x => x.StorageId)
      .GreaterThan(0)
      .When(x => x.StorageId.HasValue)
      .WithMessage("Storage ID must be greater than 0");

    RuleFor(x => x.ParentFolderId)
      .GreaterThan(0)
      .When(x => x.ParentFolderId.HasValue)
      .WithMessage("Parent folder ID must be greater than 0");

    RuleFor(x => x.SearchTerm)
      .MaximumLength(100)
      .When(x => !string.IsNullOrEmpty(x.SearchTerm))
      .WithMessage("Search term must not exceed 100 characters");
  }
}
