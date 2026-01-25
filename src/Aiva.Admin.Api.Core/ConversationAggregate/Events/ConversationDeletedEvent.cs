namespace Aiva.Admin.Api.Core.ConversationAggregate.Events;

using UserAggregate;

public sealed class ConversationDeletedEvent(ConversationId conversationId, UserId userId) : DomainEventBase
{
  public ConversationId ConversationId { get; } = conversationId;
  public UserId UserId { get; } = userId;
}
