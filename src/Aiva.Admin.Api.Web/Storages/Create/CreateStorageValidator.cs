using FluentValidation;

namespace Aiva.Admin.Api.Web.Storages.Create;

using Core.StorageAggregate;

public class CreateStorageValidator : Validator<CreateStorageRequest>
{
  public CreateStorageValidator()
  {
    RuleFor(x => x.StorageName)
      .NotEmpty()
      .WithMessage("Name is required.")
      .MinimumLength(2)
      .MaximumLength(StorageName.MaxLength);
  }
}
