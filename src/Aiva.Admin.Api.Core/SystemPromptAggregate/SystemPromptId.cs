using Vogen;

namespace Aiva.Admin.Api.Core.SystemPromptAggregate;

[ValueObject<int>]
public readonly partial struct SystemPromptId
{
  public static SystemPromptId New() => From(0);

  private static Validation Validate(int value)
      => value > 0 ? Validation.Ok : Validation.Invalid("SystemPromptId must be positive.");
}
