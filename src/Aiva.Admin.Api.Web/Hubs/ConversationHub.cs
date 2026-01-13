namespace Aiva.Admin.Api.Web.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class ConversationHub : Hub
{
  private readonly ILogger<ConversationHub> _logger;

  public ConversationHub(ILogger<ConversationHub> logger)
  {
    _logger = logger;
  }

  public override async Task OnConnectedAsync()
  {
    var userId = Context.User?.FindFirst("internal_user_id")?.Value
                 ?? Context.User?.FindFirst("sub")?.Value;

    if (!string.IsNullOrEmpty(userId))
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
      _logger.LogInformation("User {UserId} connected to ConversationHub", userId);
    }

    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    var userId = Context.User?.FindFirst("oid")?.Value
                 ?? Context.User?.FindFirst("sub")?.Value;

    if (!string.IsNullOrEmpty(userId))
    {
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    await base.OnDisconnectedAsync(exception);
  }
}

