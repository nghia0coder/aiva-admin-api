using Vogen;

namespace Aiva.Admin.Api.Core.FolderAggregate;

[ValueObject<int>]
public readonly partial struct FolderId
{
  private static Validation Validate(int value)
      => value > 0 ? Validation.Ok : Validation.Invalid("FolderId must be positive.");
}
