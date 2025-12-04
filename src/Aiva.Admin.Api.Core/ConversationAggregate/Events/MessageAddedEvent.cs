namespace Aiva.Admin.Api.Core.ConversationAggregate.Events;

public sealed class MessageAddedEvent(Conversation conversation, ChatMessage message) : DomainEventBase
{
  public Conversation Conversation { get; } = conversation;
  public ChatMessage Message { get; } = message;
}
