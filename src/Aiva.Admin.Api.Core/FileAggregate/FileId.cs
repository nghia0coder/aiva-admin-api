using Vogen;

namespace Aiva.Admin.Api.Core.FileAggregate;

[ValueObject<int>]
public readonly partial struct FileId
{
  private static Validation Validate(int value) =>
      value > 0 ? Validation.Ok : Validation.Invalid("FileId must be greater than 0");
}
