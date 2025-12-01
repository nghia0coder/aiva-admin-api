using Vogen;

namespace Aiva.Admin.Api.Core.FileAggregate;

[ValueObject<string>]
public readonly partial struct FileName
{
  public const int MaxLength = 255;

  private static Validation Validate(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return Validation.Invalid("File name cannot be empty");

    if (value.Length > MaxLength)
      return Validation.Invalid($"File name cannot exceed {MaxLength} characters");

    return Validation.Ok;
  }
}
