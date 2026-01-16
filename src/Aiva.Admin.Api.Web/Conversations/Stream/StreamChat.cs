using System.Text.Json;
using Ardalis.SharedKernel;

namespace Aiva.Admin.Api.Web.Conversations.Stream;

using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;
using Core.SystemPromptAggregate;

public class StreamChat(
    IRepository<Conversation> repository,
    IChatCompletionService chatService,
    ISystemPromptService systemPromptService,
    IRetrievalService retrievalService)
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

    // *** RAG: Retrieve relevant context ***
    var retrievalResult = await retrievalService.RetrieveContextAsync(
        request.Message,
        new RetrievalOptions
        {
          TopK = 5,
          MinScore = 0.7,
          Strategy = SearchStrategy.Hybrid  // Use hybrid for e-commerce
        },
        ct);

    var messagesWithContext = await BuildAugmentedMessagesAsync(
        conversation,
        retrievalResult.IsSuccess ? retrievalResult.Value.FormattedContext : null,
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
