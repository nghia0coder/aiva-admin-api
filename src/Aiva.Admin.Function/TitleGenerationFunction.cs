
using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Ardalis.SharedKernel;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Functions;

public class TitleGenerationFunction
{
  private readonly IServiceScopeFactory _scopeFactory;

  public TitleGenerationFunction(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory;
  }

  [Function("GenerateConversationTitle")]
  public async Task GenerateTitle(
      [ServiceBusTrigger("title-generation-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
      FunctionContext context)
  {
    var logger = context.GetLogger("GenerateConversationTitle");

    try
    {
      var titleRequest = JsonSerializer.Deserialize<TitleGenerationMessage>(message.Body.ToString());
      var conversationId = ConversationId.From(titleRequest!.ConversationId);

      using var scope = _scopeFactory.CreateScope();
      var repository = scope.ServiceProvider.GetRequiredService<IRepository<Conversation>>();
      var titleService = scope.ServiceProvider.GetRequiredService<ITitleGenerationService>();

      // Get conversation
      var spec = new ConversationByIdWithMessagesSpec(conversationId);
      var conversation = await repository.FirstOrDefaultAsync(spec);

      if (conversation == null)
      {
        logger.LogWarning("Conversation {ConversationId} not found", conversationId.Value);
        return;
      }

      // Check if title already generated (avoid duplicate processing)
      if (!string.IsNullOrEmpty(conversation.Title) && conversation.Title != "New Conversation")
      {
        logger.LogInformation("Title already exists for conversation {ConversationId}", conversationId.Value);
        return;
      }

      // Check if conversation has messages for title generation
      if (conversation.Messages.Count == 0)
      {
        logger.LogWarning("Conversation {ConversationId} has no messages for title generation", conversationId.Value);
        return;
      }

      // Generate title
      var result = await titleService.GenerateTitleAsync(conversation.Messages);

      if (result.IsSuccess)
      {
        conversation.SetGeneratedTitle(result.Value);
        await repository.UpdateAsync(conversation);

        logger.LogInformation(
            "Generated title for conversation {ConversationId}: '{Title}'",
            conversationId.Value,
            result.Value);
      }
      else
      {
        logger.LogWarning(
            "Failed to generate title for {ConversationId}: {Errors}",
            conversationId.Value,
            string.Join(", ", result.Errors));

        // Let the message retry via Service Bus retry policy
        throw new InvalidOperationException($"Title generation failed: {string.Join(", ", result.Errors)}");
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error generating conversation title");
      throw; // Triggers retry mechanism
    }
  }
}

public record TitleGenerationMessage(Guid ConversationId, DateTime RequestedAt);
