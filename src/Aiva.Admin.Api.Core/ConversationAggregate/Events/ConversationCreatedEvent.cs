namespace Aiva.Admin.Api.Core.ConversationAggregate.Events;

public sealed class ConversationCreatedEvent(Conversation conversation) : DomainEventBase
{
  public Conversation Conversation { get; } = conversation;
}
