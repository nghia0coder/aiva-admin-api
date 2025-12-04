using Vogen;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

[ValueObject<Guid>]
public readonly partial struct MessageId
{
  public static MessageId New() => From(Guid.NewGuid());

  private static Validation Validate(Guid value)
      => value != Guid.Empty
          ? Validation.Ok
          : Validation.Invalid("MessageId cannot be empty.");
}
