using Vogen;

namespace Aiva.Admin.Api.Core.StorageAggregate;

[ValueObject<int>]
public readonly partial struct StorageId
{
  private static Validation Validate(int value)
      => value > 0 ? Validation.Ok : Validation.Invalid("StorageId must be positive.");
}
