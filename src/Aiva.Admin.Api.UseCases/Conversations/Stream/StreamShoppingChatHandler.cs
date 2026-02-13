using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamShoppingChatHandler(
IRepository<Conversation> repository,
// TODO: Enable when implementing AI features
// IChatCompletionService chatService,
ISystemPromptService systemPromptService,
IChatHistoryService chatHistoryService,
ILogger<StreamShoppingChatHandler> logger)
: IRequestHandler<StreamShoppingChatCommand, Result<StreamShoppingChatResponse>>
{
  public async ValueTask<Result<StreamShoppingChatResponse>> Handle(StreamShoppingChatCommand request, CancellationToken cancellationToken)
  {
    try
    {
      // TODO: Implement complete shopping assistant logic
      
      var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(request.ConversationId));
      var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

      if (conversation is null)
      {
        return Result.NotFound("Conversation not found");
      }

      var recentMessages = conversation.GetRecentMessages();
      var chatHistory = await chatHistoryService.SerializeChatHistoryAsync(recentMessages.ToList());

      conversation.AddMessage(ChatRole.User, request.Message);

      // TODO: Get shopping assistant system prompt
      var systemPrompt = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("shopping-assistant"),
          cancellationToken);

      // TODO: Implement shopping-specific logic:
      // 1. Product search and recommendations
      // 2. Price comparison
      // 3. Shopping advice
      // 4. Integration with e-commerce APIs
      // 5. Inventory management
      // 6. Customer preference learning

      var response = new StreamShoppingChatResponse
      {
        TextResponse = "Shopping Assistant is under development. Here's what I plan to help you with:\n" +
                      "🛍️ Product recommendations\n" +
                      "💰 Price comparisons\n" +
                      "📊 Shopping advice\n" +
                      "🎯 Personalized suggestions\n" +
                      "\nComing soon!",
        HasProducts = false
      };

      // TODO: Replace with actual AI response
      // var aiResponse = await chatService.GetCompletionAsync(systemPrompt, request.Message);
      // response.TextResponse = aiResponse;

      // Save assistant message
      conversation.AddMessage(ChatRole.Assistant, response.TextResponse);

      if (conversation.IsReadyForTitleGeneration())
      {
        conversation.QueueForTitleGeneration();
      }

      await repository.UpdateAsync(conversation, cancellationToken);

      return Result.Success(response);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat stream for conversation {ConversationId}", request.ConversationId);
      return Result.Error(ex.Message);
    }
  }
}
