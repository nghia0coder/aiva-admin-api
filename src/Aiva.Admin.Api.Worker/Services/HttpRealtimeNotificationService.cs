using System.Net.Http.Json;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.UserAggregate;
using Microsoft.Extensions.Configuration;

namespace Aiva.Admin.Api.Worker.Services;

public class HttpRealtimeNotificationService : IRealtimeNotificationService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<HttpRealtimeNotificationService> _logger;
  private readonly IConfiguration _configuration;
  private const string ApiKeyHeaderName = "X-Internal-Api-Key";

  public HttpRealtimeNotificationService(
      HttpClient httpClient,
      ILogger<HttpRealtimeNotificationService> logger,
      IConfiguration configuration)
  {
    _httpClient = httpClient;
    _logger = logger;
    _configuration = configuration;
  }

  public async Task NotifyTitleUpdatedAsync(
      ConversationId conversationId,
      UserId userId,
      string newTitle,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var payload = new
      {
        ConversationId = conversationId.Value,
        UserId = userId.Value,
        NewTitle = newTitle
      };

      var request = new HttpRequestMessage(HttpMethod.Post, "/internal/notifications/title-updated")
      {
        Content = JsonContent.Create(payload)
      };

      // Add internal API key for authentication
      var apiKey = GetInternalApiKey();
      request.Headers.Add(ApiKeyHeaderName, apiKey);

      var response = await _httpClient.SendAsync(request, cancellationToken);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation(
          "Successfully sent TitleUpdated notification for conversation {ConversationId} to Web API",
          conversationId.Value);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
          "Failed to send real-time notification via HTTP for conversation {ConversationId}",
          conversationId.Value);
      // We do NOT throw here to avoid failing the background job just because notification failed
    }
  }

  private string GetInternalApiKey()
  {
    var apiKey = _configuration["AppSettings:Internal:ApiKey"];
    
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      throw new InvalidOperationException(
          "Internal API key not configured. Please set AppSettings:Internal:ApiKey in configuration.");
    }

    return apiKey;
  }
}
