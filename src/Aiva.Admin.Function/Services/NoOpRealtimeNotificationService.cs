using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.UserAggregate;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Function.Services;

public class NoOpRealtimeNotificationService : IRealtimeNotificationService
{
  private readonly ILogger<NoOpRealtimeNotificationService> _logger;

  public NoOpRealtimeNotificationService(ILogger<NoOpRealtimeNotificationService> logger)
  {
    _logger = logger;
  }

  public Task NotifyTitleUpdatedAsync(ConversationId conversationId, UserId userId, string newTitle, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation(
        "Title updated notification (no-op): ConversationId={ConversationId}, NewTitle='{NewTitle}'",
        conversationId.Value,
        newTitle);

    return Task.CompletedTask;
  }
}
