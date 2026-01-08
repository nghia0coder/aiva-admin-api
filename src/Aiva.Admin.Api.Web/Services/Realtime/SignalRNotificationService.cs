namespace Aiva.Admin.Api.Web.Services.Realtime;

using Aiva.Admin.Api.Web.Hubs;

using Core.ConversationAggregate;
using Core.Interfaces;
using Core.UserAggregate;
using Microsoft.AspNetCore.SignalR;


public class SignalRNotificationService : IRealtimeNotificationService
{
  private readonly IHubContext<ConversationHub> _hubContext;
  private readonly ILogger<SignalRNotificationService> _logger;

  public SignalRNotificationService(
      IHubContext<ConversationHub> hubContext,
      ILogger<SignalRNotificationService> logger)
  {
    _hubContext = hubContext;
    _logger = logger;
  }

  public async Task NotifyTitleUpdatedAsync(
      ConversationId conversationId,
      UserId userId,
      string newTitle,
      CancellationToken cancellationToken = default)
  {
    var payload = new TitleUpdatedMessage(
        conversationId.Value,
        newTitle,
        DateTime.UtcNow);

    await _hubContext.Clients
        .Group($"user_{userId.Value}")
        .SendAsync("TitleUpdated", payload, cancellationToken);

    _logger.LogDebug(
        "Sent TitleUpdated notification for conversation {ConversationId}",
        conversationId.Value);
  }
}

public record TitleUpdatedMessage(
    Guid ConversationId,
    string NewTitle,
    DateTime UpdatedAt);
