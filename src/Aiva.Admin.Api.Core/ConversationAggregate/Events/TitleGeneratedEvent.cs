using Aiva.Admin.Api.Core.UserAggregate;

namespace Aiva.Admin.Api.Core.ConversationAggregate.Events;

public sealed class TitleGeneratedEvent(Conversation conversation, string generatedTitle) : DomainEventBase
{
  public ConversationId ConversationId { get; } = conversation.Id;
  public UserId UserId { get; } = conversation.UserId;
  public string GeneratedTitle { get; } = generatedTitle;
}
