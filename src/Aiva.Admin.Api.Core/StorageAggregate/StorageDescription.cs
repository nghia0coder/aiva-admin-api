using Vogen;

namespace Aiva.Admin.Api.Core.StorageAggregate;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public partial struct StorageDescription
{
  public const int MaxLength = 500;
  private static Validation Validate(in string description) =>
    string.IsNullOrEmpty(description)
      ? Validation.Invalid("Description cannot be empty")
      : description.Length > MaxLength
        ? Validation.Invalid($"Description cannot be longer than {MaxLength} characters")
        : Validation.Ok;
}
