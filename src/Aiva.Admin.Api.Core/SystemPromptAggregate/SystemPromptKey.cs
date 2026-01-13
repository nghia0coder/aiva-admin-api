using Vogen;

namespace Aiva.Admin.Api.Core.SystemPromptAggregate;

[ValueObject<string>]
public readonly partial struct SystemPromptKey
{
  public const int MaxLength = 100;

  private static Validation Validate(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return Validation.Invalid("SystemPromptKey cannot be empty.");

    if (value.Length > MaxLength)
      return Validation.Invalid($"SystemPromptKey cannot exceed {MaxLength} characters.");

    // Only allow alphanumeric, hyphens, and underscores
    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9_-]+$"))
      return Validation.Invalid("SystemPromptKey can only contain letters, numbers, hyphens, and underscores.");

    return Validation.Ok;
  }
}
