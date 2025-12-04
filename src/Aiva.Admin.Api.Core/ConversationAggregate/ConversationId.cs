using Vogen;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

[ValueObject<Guid>]
public readonly partial struct ConversationId
{
  public static ConversationId New() => From(Guid.NewGuid());

  private static Validation Validate(Guid value)
      => value != Guid.Empty
          ? Validation.Ok
          : Validation.Invalid("ConversationId cannot be empty.");
}
