namespace Aiva.Admin.Api.Core.ConversationAggregate.Handlers;

using Events;

public class ConversationCreatedHandler(ILogger<ConversationCreatedHandler> logger)
    : INotificationHandler<ConversationCreatedEvent>
{
  public ValueTask Handle(ConversationCreatedEvent domainEvent, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Conversation created: {ConversationId}",
        domainEvent.Conversation.Id);

    return ValueTask.CompletedTask;
  }
}
