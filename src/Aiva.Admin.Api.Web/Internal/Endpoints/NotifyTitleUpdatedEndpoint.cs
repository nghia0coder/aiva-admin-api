using Microsoft.AspNetCore.Authorization;

namespace Aiva.Admin.Api.Web.Internal.Endpoints;

using Core.ConversationAggregate;
using Core.Interfaces;
using Core.UserAggregate;

public record NotifyTitleUpdatedRequest(ConversationId ConversationId, UserId UserId, string NewTitle);

[HttpPost("/internal/notifications/title-updated")]
[AllowAnonymous]
public class NotifyTitleUpdatedEndpoint : Endpoint<NotifyTitleUpdatedRequest>
{
  private readonly IRealtimeNotificationService _notificationService;
  private readonly ILogger<NotifyTitleUpdatedEndpoint> _logger;

  public NotifyTitleUpdatedEndpoint(
      IRealtimeNotificationService notificationService,
      ILogger<NotifyTitleUpdatedEndpoint> logger)
  {
    _notificationService = notificationService;
    _logger = logger;
  }

  public override async Task HandleAsync(NotifyTitleUpdatedRequest req, CancellationToken ct)
  {
    // Validate internal API key
    if (!ValidateInternalApiKey())
    {
      _logger.LogWarning(
          "Unauthorized request to internal endpoint {Path} - missing or invalid API key",
          HttpContext.Request.Path);

      HttpContext.Response.StatusCode = 401;
      await HttpContext.Response.WriteAsync("Unauthorized", ct);
      return;
    }

    _logger.LogInformation(
        "Received internal notification request for Conversation {ConversationId}",
        req.ConversationId);

    await _notificationService.NotifyTitleUpdatedAsync(
        req.ConversationId,
        req.UserId,
        req.NewTitle,
        ct);

    await Send.OkAsync();
  }

  private bool ValidateInternalApiKey()
  {
    const string ApiKeyHeaderName = "X-Internal-Api-Key";

    if (!HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKeyValues))
    {
      return false;
    }

    var extractedApiKey = extractedApiKeyValues.ToString();
    var configuredApiKey = HttpContext.RequestServices
        .GetRequiredService<IConfiguration>()["AppSettings:Internal:ApiKey"];

    if (string.IsNullOrWhiteSpace(configuredApiKey))
    {
      _logger.LogError("Internal API key not configured in appsettings. Please configure AppSettings:Internal:ApiKey");
      return false;
    }

    return configuredApiKey.Equals(extractedApiKey, StringComparison.Ordinal);
  }
}
