namespace Aiva.Admin.Api.Core.ConversationAggregate.Handlers;

using Events;

public class TitleGenerationQueuedHandler(ILogger<TitleGenerationQueuedHandler> logger)
    : INotificationHandler<TitleGenerationQueuedEvent>
{
  public ValueTask Handle(TitleGenerationQueuedEvent domainEvent, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Conversation {ConversationId} queued for title generation",
        domainEvent.ConversationId.Value);

    return ValueTask.CompletedTask;
  }
}
