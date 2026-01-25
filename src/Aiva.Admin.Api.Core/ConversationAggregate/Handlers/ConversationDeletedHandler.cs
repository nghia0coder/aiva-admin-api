namespace Aiva.Admin.Api.Core.ConversationAggregate.Handlers;

using Events;

public class ConversationDeletedHandler(ILogger<ConversationDeletedHandler> logger)
    : INotificationHandler<ConversationDeletedEvent>
{
  public ValueTask Handle(ConversationDeletedEvent domainEvent, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Conversation {ConversationId} deleted for user {UserId}",
        domainEvent.ConversationId.Value,
        domainEvent.UserId.Value);

    return ValueTask.CompletedTask;
  }
}
