namespace Aiva.Admin.Api.Core.ConversationAggregate.Handlers;

using Events;
using Interfaces;

public class TitleGeneratedHandler : INotificationHandler<TitleGeneratedEvent>
{
  private readonly IRealtimeNotificationService _notificationService;
  private readonly ILogger<TitleGeneratedHandler> _logger;

  public TitleGeneratedHandler(
      IRealtimeNotificationService notificationService,
      ILogger<TitleGeneratedHandler> logger)
  {
    _notificationService = notificationService;
    _logger = logger;
  }

  public async ValueTask Handle(
      TitleGeneratedEvent domainEvent,
      CancellationToken cancellationToken)
  {
    _logger.LogInformation(
        "Title generated for conversation {ConversationId}: '{Title}'",
        domainEvent.ConversationId.Value,
        domainEvent.GeneratedTitle);

    await _notificationService.NotifyTitleUpdatedAsync(
        domainEvent.ConversationId,
        domainEvent.UserId,
        domainEvent.GeneratedTitle,
        cancellationToken);
  }
}
