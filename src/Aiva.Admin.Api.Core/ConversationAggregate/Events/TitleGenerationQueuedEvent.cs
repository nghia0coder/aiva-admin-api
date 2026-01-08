namespace Aiva.Admin.Api.Core.ConversationAggregate.Events;

public sealed class TitleGenerationQueuedEvent(Conversation conversation) : DomainEventBase
{
  public ConversationId ConversationId { get; } = conversation.Id;
}
