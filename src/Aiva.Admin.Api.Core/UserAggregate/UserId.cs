using Vogen;

namespace Aiva.Admin.Api.Core.UserAggregate;

[ValueObject<int>]
public readonly partial struct UserId
{
  public static UserId New() => From(0);
  private static Validation Validate(int value)
      => value > 0 ? Validation.Ok : Validation.Invalid("UserId must be positive.");
}
