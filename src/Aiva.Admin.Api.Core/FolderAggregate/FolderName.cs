using Vogen;

namespace Aiva.Admin.Api.Core.FolderAggregate;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public partial struct FolderName
{
  public const int MaxLength = 255;

  private static Validation Validate(in string name)
  {
    if (string.IsNullOrWhiteSpace(name))
      return Validation.Invalid("Folder name cannot be empty");

    if (name.Length > MaxLength)
      return Validation.Invalid($"Folder name cannot be longer than {MaxLength} characters");

    // Azure Blob naming restrictions - không cho phép các ký tự đặc biệt
    var invalidChars = new[] { '\\', ':', '*', '?', '"', '<', '>', '|' };
    if (name.IndexOfAny(invalidChars) >= 0)
      return Validation.Invalid("Folder name contains invalid characters");

    return Validation.Ok;
  }
}
