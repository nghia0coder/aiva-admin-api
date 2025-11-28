using Aiva.Admin.Api.Core.FolderAggregate;
using FluentValidation;

namespace Aiva.Admin.Api.Web.Folders.Create;

public class CreateFolderValidator : Validator<CreateFolderRequest>
{
  // Azure Blob naming restrictions
  private static readonly char[] InvalidChars = ['\\', ':', '*', '?', '"', '<', '>', '|'];

  public CreateFolderValidator()
  {
    RuleFor(x => x.FolderName)
      .NotEmpty()
      .WithMessage("Folder name is required.")
      .MaximumLength(FolderName.MaxLength)
      .WithMessage($"Folder name cannot be longer than {FolderName.MaxLength} characters.")
      .Must(name => name is null || name.IndexOfAny(InvalidChars) < 0)
      .WithMessage("Folder name contains invalid characters (\\, :, *, ?, \", <, >, |).");

    RuleFor(x => x.StorageId)
      .GreaterThan(0)
      .WithMessage("StorageId must be a positive number.");

    RuleFor(x => x.ParentFolderId)
      .GreaterThan(0)
      .When(x => x.ParentFolderId.HasValue)
      .WithMessage("ParentFolderId must be a positive number.");
  }
}
