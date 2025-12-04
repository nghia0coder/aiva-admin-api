using System.Text.Json;
using Ardalis.SharedKernel;

namespace Aiva.Admin.Api.Web.Conversations.Stream;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;

public class StreamChatRequest
{
  public const string Route = "/conversations/{ConversationId}/stream";
  public Guid ConversationId { get; set; }
  public string Message { get; set; } = string.Empty;
}

public class StreamChat(
    IRepository<Conversation> repository,
    IChatCompletionService chatService)
    : Endpoint<StreamChatRequest>
{
  public override void Configure()
  {
    Post(StreamChatRequest.Route);
    AllowAnonymous();
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

    var fullResponse = new System.Text.StringBuilder();

    await foreach (var chunkResult in chatService.StreamCompletionAsync(conversation.Messages, ct))
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
    await repository.UpdateAsync(conversation, ct);

    await SendEventAsync("done", new { complete = true }, ct);
  }

  private async Task SendEventAsync<T>(string eventType, T data, CancellationToken ct)
  {
    var json = JsonSerializer.Serialize(data);
    await HttpContext.Response.WriteAsync($"event: {eventType}\n", ct);
    await HttpContext.Response.WriteAsync($"data: {json}\n\n", ct);
    await HttpContext.Response.Body.FlushAsync(ct);
  }
}
