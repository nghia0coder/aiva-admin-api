namespace Aiva.Admin.Api.Worker.Services;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;
using Infrastructure.Configuration;

/// <summary>
/// Background service that processes queued conversations for title generation.
/// Runs independently from the API, allowing non-blocking title generation.
/// </summary>
public sealed class TitleGenerationBackgroundService : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<TitleGenerationBackgroundService> _logger;
  private readonly AppSettings _appSettings;

  public TitleGenerationBackgroundService(
      IServiceScopeFactory scopeFactory,
      AppSettings appSettings,
      ILogger<TitleGenerationBackgroundService> logger)
  {
    _scopeFactory = scopeFactory;
    _appSettings = appSettings;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var config = _appSettings.TitleGeneration;

    _logger.LogInformation(
        "Title Generation Worker started. Polling every {Interval}s, BatchSize: {BatchSize}",
        config.PollingIntervalSeconds,
        config.BatchSize);

    if (!config.Enabled)
    {
      _logger.LogWarning("Title Generation Worker is DISABLED via configuration");
      return;
    }

    // Initial delay to allow services to start
    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var processedCount = await ProcessQueuedConversationsAsync(stoppingToken);

        if (processedCount > 0)
        {
          _logger.LogInformation(
              "Generated titles for {Count} conversations",
              processedCount);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in title generation worker loop");
      }

      await Task.Delay(
          TimeSpan.FromSeconds(config.PollingIntervalSeconds),
          stoppingToken);
    }

    _logger.LogInformation("Title Generation Worker stopped");
  }

  private async Task<int> ProcessQueuedConversationsAsync(CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();

    var repository = scope.ServiceProvider
        .GetRequiredService<IRepository<Conversation>>();
    var titleService = scope.ServiceProvider
        .GetRequiredService<ITitleGenerationService>();

    var config = _appSettings.TitleGeneration;

    // Get queued conversations
    var spec = new QueuedConversationsForTitleGenerationSpec(config.BatchSize);
    var queuedConversations = await repository.ListAsync(spec, cancellationToken);

    if (queuedConversations.Count == 0)
    {
      return 0;
    }

    _logger.LogDebug(
        "Found {Count} conversations queued for title generation",
        queuedConversations.Count);

    var processedCount = 0;

    foreach (var conversation in queuedConversations)
    {
      try
      {
        var result = await titleService.GenerateTitleAsync(
            conversation.Messages,
            cancellationToken);

        if (result.IsSuccess)
        {
          conversation.SetGeneratedTitle(result.Value);
          await repository.UpdateAsync(conversation, cancellationToken);
          processedCount++;

          _logger.LogInformation(
              "Generated title for conversation {ConversationId}: '{Title}'",
              conversation.Id.Value,
              result.Value);
        }
        else
        {
          _logger.LogWarning(
              "Failed to generate title for {ConversationId}: {Errors}",
              conversation.Id.Value,
              string.Join(", ", result.Errors));

          // Keep as Queued for retry on next poll
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex,
            "Exception while generating title for {ConversationId}",
            conversation.Id.Value);
      }
    }

    return processedCount;
  }
}
