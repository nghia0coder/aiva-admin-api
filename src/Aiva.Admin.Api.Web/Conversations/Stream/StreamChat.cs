using System.Text.Json;
using Ardalis.SharedKernel;

namespace Aiva.Admin.Api.Web.Conversations.Stream;

using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;
using Core.SystemPromptAggregate;
using Infrastructure.Configuration;

public class StreamChat(
    IRepository<Conversation> repository,
    IChatCompletionService chatService,
    ISystemPromptService systemPromptService,
    IRetrievalService retrievalService,
    IIntentDetectionService intentDetectionService,
    IResponseFormatterService responseFormatterService,
    AppSettings appSettings,
    ILogger<StreamChat> logger)
    : Endpoint<StreamChatRequest>
{
  public override void Configure()
  {
    Post(StreamChatRequest.Route);
    Summary(s =>
    {
      s.Summary = "Stream AI response in real-time";
      s.Description = "Sends a message and streams the AI response using Server-Sent Events (SSE).";
    });
    Tags("Conversations");
  }

  public override async Task HandleAsync(StreamChatRequest request, CancellationToken ct)
  {
    HttpContext.Response.Headers.ContentType = "text/event-stream";
    HttpContext.Response.Headers.CacheControl = "no-cache";
    HttpContext.Response.Headers.Connection = "keep-alive";

    var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(request.ConversationId));
    var conversation = await repository.FirstOrDefaultAsync(spec, ct);

    if (conversation is null)
    {
      await SendEventAsync("error", new { message = "Conversation not found" }, ct);
      return;
    }

    // Add user message
    conversation.AddMessage(ChatRole.User, request.Message);

    // Detect Intent
    var intentDetectionSettings = appSettings.IntentDetection;
    IntentDetectionResult? intentResult = null;

    if (intentDetectionSettings.Enabled)
    {
      logger.LogInformation("Detecting intent for query: {Query}", request.Message);

      var intentDetectionResult = await intentDetectionService.DetectIntentAsync(
          request.Message,
          conversation.Messages.ToList(),
          ct);

      if (intentDetectionResult.IsSuccess)
      {
        intentResult = intentDetectionResult.Value;
        logger.LogInformation(
            "Intent detected: {Intent} (Confidence: {Confidence:F2}, RequiresStructured: {RequiresStructured})",
            intentResult.Intent.Name,
            intentResult.Confidence,
            intentResult.RequiresStructuredResponse);
      }
      else
      {
        logger.LogWarning("Intent detection failed: {Errors}", string.Join(", ", intentDetectionResult.Errors));
      }
    }

    // Retrieve relevant context
    var retrievalSettings = appSettings.Retrieval;

    // Use hybrid-specific threshold if available, otherwise fall back to default threshold
    var searchStrategy = SearchStrategy.Hybrid;
    var minScoreForSearch = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold;
    var minScoreForValidation = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold;

    logger.LogInformation(
        "Starting retrieval for query: {Query}. Strategy: {Strategy}, TopK: {TopK}, MinScore: {MinScore} (Hybrid threshold: {HybridThreshold})",
        request.Message,
        searchStrategy,
        retrievalSettings.TopK,
        minScoreForSearch,
        retrievalSettings.HybridSearchMinScoreThreshold);

    var retrievalResult = await retrievalService.RetrieveContextAsync(
        request.Message,
        new RetrievalOptions
        {
          TopK = retrievalSettings.TopK,
          MinScore = minScoreForSearch,
          Strategy = searchStrategy  // Use hybrid for e-commerce
        },
        ct);

    // Log retrieval result details
    if (retrievalResult.IsSuccess)
    {
      // Log individual result scores for debugging
      if (retrievalResult.Value.Results.Count > 0)
      {
        var scoreDetails = string.Join(", ",
            retrievalResult.Value.Results.Select((r, i) => $"#{i + 1}: {r.Score:F4}"));
        logger.LogDebug("Retrieved result scores: {Scores}", scoreDetails);
      }
    }
    else
    {
      logger.LogError(
          "Retrieval failed. Query: {Query}, Errors: {Errors}",
          request.Message,
          string.Join(", ", retrievalResult.Errors));
    }

    // *** GATE CHECK: Out-of-Scope Detection ***
    if (retrievalSettings.EnableOutOfScopeDetection &&
        (!retrievalResult.IsSuccess ||
         !retrievalResult.Value.HasSufficientContext(
             minScoreForValidation,
             retrievalSettings.MinResultCount)))
    {
      // No relevant context found - return standard out-of-scope response without calling LLM
      var outOfScopeMessage = OutOfScopeResponse.Default;

      // Stream the out-of-scope response (for consistent UX)
      await SendEventAsync("message", new { content = outOfScopeMessage }, ct);

      // Save the response to conversation history
      conversation.AddMessage(ChatRole.Assistant, outOfScopeMessage);
      await repository.UpdateAsync(conversation, ct);

      await SendEventAsync("done", new { complete = true, outOfScope = true }, ct);
      return;
    }
    // *** END GATE CHECK ***

    // Check if structured response is needed
    var shouldUseStructuredResponse = intentResult != null
        && intentResult.RequiresStructuredResponse
        && (intentResult.Intent == QueryIntent.Browse
            || intentResult.Intent == QueryIntent.Compare
            || intentResult.Intent == QueryIntent.Purchase)
        && retrievalResult.Value.Results.Count > 0;

    if (shouldUseStructuredResponse)
    {
      logger.LogInformation(
          "Using structured response for intent: {Intent} with {Count} results",
          intentResult!.Intent.Name,
          retrievalResult.Value.Results.Count);

      // Format structured response
      var formatResult = await responseFormatterService.FormatStructuredResponseAsync(
          intentResult.Intent,
          retrievalResult.Value.Results,
          request.Message,
          ct);

      if (formatResult.IsSuccess)
      {
        var package = formatResult.Value;

        // Stream chatbot suggestion text first
        await SendEventAsync("message", new { content = package.ChatbotSuggestionText }, ct);

        // Send table metadata and columns first
        await SendEventAsync("structured_data_start", new
        {
          metadata = new
          {
            title = package.TableData.Metadata.Title,
            description = package.TableData.Metadata.Description,
            totalCount = package.TableData.Metadata.TotalCount,
            displayedCount = package.TableData.Metadata.DisplayedCount
          },
          columns = package.TableData.Columns.Select(c => new
          {
            key = c.Key,
            label = c.Label,
            type = c.Type.ToString(),
            sortable = c.Sortable,
            filterable = c.Filterable
          })
        }, ct);

        // Stream each row individually for progressive rendering
        foreach (var row in package.TableData.Rows)
        {
          await SendEventAsync("structured_data_row", new
          {
            id = row.Id,
            cells = row.Cells,
            actions = row.Actions.Select(a => new
            {
              type = a.Type.ToString(),
              label = a.Label,
              icon = a.Icon,
              endpoint = a.Endpoint,
              method = a.Method,
              @params = a.Params,
              isDisabled = a.IsDisabled,
              disabledReason = a.DisabledReason
            })
          }, ct);

          // Optional: Add small delay between rows for smoother animation
          await Task.Delay(50, ct);
        }

        // Send global actions last
        await SendEventAsync("structured_data_complete", new
        {
          globalActions = package.TableData.GlobalActions.Select(a => new
          {
            type = a.Type.ToString(),
            label = a.Label,
            icon = a.Icon,
            endpoint = a.Endpoint,
            method = a.Method,
            @params = a.Params,
            isDisabled = a.IsDisabled,
            disabledReason = a.DisabledReason
          })
        }, ct);

        // Save assistant message with structured data
        var messageContent = package.ChatbotSuggestionText;
        var message = conversation.AddMessage(ChatRole.Assistant, messageContent);

        // Store structured data as JSON in the message
        message.SetStructuredResponse(package.TableData);

        await repository.UpdateAsync(conversation, ct);

        await SendEventAsync("done", new { complete = true, hasStructuredData = true }, ct);
        return;
      }
      else
      {
        logger.LogWarning(
            "Structured response formatting failed: {Errors}. Falling back to text response.",
            string.Join(", ", formatResult.Errors));
        // Fall through to normal text streaming
      }
    }

    // Normal text streaming
    var messagesWithContext = await BuildAugmentedMessagesAsync(
        conversation,
        retrievalResult.Value.FormattedContext,
        ct);

    var fullResponse = new System.Text.StringBuilder();

    await foreach (var chunkResult in chatService.StreamCompletionAsync(messagesWithContext, ct))
    {
      if (chunkResult.IsSuccess)
      {
        fullResponse.Append(chunkResult.Value);
        await SendEventAsync("message", new { content = chunkResult.Value }, ct);
      }
      else
      {
        await SendEventAsync("error", new { message = chunkResult.Errors.FirstOrDefault() }, ct);
        return;
      }
    }

    // Save assistant message after streaming completes
    conversation.AddMessage(ChatRole.Assistant, fullResponse.ToString());

    // Check if conversation is ready for title generation (migrated from SendMessageHandler)
    if (conversation.IsReadyForTitleGeneration())
    {
      conversation.QueueForTitleGeneration();
    }

    await repository.UpdateAsync(conversation, ct);

    await SendEventAsync("done", new { complete = true }, ct);
  }

  private async Task<IReadOnlyList<ChatMessage>> BuildAugmentedMessagesAsync(
      Conversation conversation,
      string? context,
      CancellationToken cancellationToken)
  {
    var messages = conversation.Messages.ToList();

    // If conversation doesn't have a system prompt, get default from database
    if (!messages.Any(m => m.Role == ChatRole.System))
    {
      var promptResult = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("default"),
          cancellationToken);

      if (promptResult.IsSuccess)
      {
        messages.Insert(0, new ChatMessage(
            ChatRole.System,
            promptResult.Value,
            conversation.Id));
      }
    }

    // Inject RAG context before the last user message
    if (!string.IsNullOrEmpty(context))
    {
      var lastUserIndex = messages.FindLastIndex(m => m.Role == ChatRole.User);
      if (lastUserIndex >= 0)
      {
        var originalContent = messages[lastUserIndex].Content;
        messages[lastUserIndex] = new ChatMessage(
            ChatRole.User,
            $"{context}\n\nQuestion: {originalContent}",
            conversation.Id);
      }
    }

    return messages;
  }

  private async Task SendEventAsync<T>(string eventType, T data, CancellationToken ct)
  {
    var json = JsonSerializer.Serialize(data);
    await HttpContext.Response.WriteAsync($"event: {eventType}\n", ct);
    await HttpContext.Response.WriteAsync($"data: {json}\n\n", ct);
    await HttpContext.Response.Body.FlushAsync(ct);
  }
}
