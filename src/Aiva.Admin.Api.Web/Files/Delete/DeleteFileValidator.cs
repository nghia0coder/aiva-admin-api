using FluentValidation;

namespace Aiva.Admin.Api.Web.Files.Delete;

/// <summary>
/// Validator for delete file requests
/// </summary>
public sealed class DeleteFileValidator : Validator<DeleteFileRequest>
{
  public DeleteFileValidator()
  {
    RuleFor(x => x.Id)
        .GreaterThan(0)
        .WithMessage("File ID must be greater than 0");
  }
}
