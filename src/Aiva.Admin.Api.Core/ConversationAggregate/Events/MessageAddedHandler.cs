namespace Aiva.Admin.Api.Core.ConversationAggregate.Handlers;

using Events;

public class MessageAddedHandler(ILogger<MessageAddedHandler> logger)
    : INotificationHandler<MessageAddedEvent>
{
  public ValueTask Handle(MessageAddedEvent domainEvent, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Message added to conversation {ConversationId}: {Role}",
        domainEvent.Conversation.Id,
        domainEvent.Message.Role.Name);

    return ValueTask.CompletedTask;
  }
}
