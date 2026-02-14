using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamShoppingChatHandler(
    IRepository<Conversation> repository,
    IShoppingChatService shoppingChatService,
    ILogger<StreamShoppingChatHandler> logger)
    : IRequestHandler<StreamShoppingChatCommand, Result<StreamShoppingChatResponse>>
{
  public async ValueTask<Result<StreamShoppingChatResponse>> Handle(
      StreamShoppingChatCommand request,
      CancellationToken cancellationToken)
  {
    try
    {
      // Validate and get conversation
      var conversation = await GetConversationAsync(request.ConversationId, cancellationToken);
      if (conversation == null)
      {
        return Result.NotFound("Conversation not found");
      }

      // Add user message to conversation
      conversation.AddMessage(ChatRole.User, request.Message);

      // Process shopping chat using dedicated service
      var shoppingResult = await shoppingChatService.ProcessShoppingChatAsync(
          conversation,
          request.Message,
          request.UserName,
          cancellationToken);

      if (!shoppingResult.IsSuccess)
      {
        return Result.Error(string.Join("; ", shoppingResult.Errors));
      }

      // Save assistant message
      conversation.AddMessage(ChatRole.Assistant, shoppingResult.Value.TextResponse);

      // Handle title generation if needed
      if (conversation.IsReadyForTitleGeneration())
      {
        conversation.QueueForTitleGeneration();
      }

      // Save conversation
      await repository.UpdateAsync(conversation, cancellationToken);

      // Map to response DTO
      var response = MapToResponse(shoppingResult.Value);

      logger.LogInformation("Successfully processed shopping chat for conversation {ConversationId}", request.ConversationId);

      return Result.Success(response);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat stream for conversation {ConversationId}", request.ConversationId);
      return Result.Error(ex.Message);
    }
  }

  private async Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken)
  {
    var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(conversationId));
    return await repository.FirstOrDefaultAsync(spec, cancellationToken);
  }

  private static StreamShoppingChatResponse MapToResponse(ShoppingChatResult result)
  {
    return new StreamShoppingChatResponse
    {
      TextResponse = result.TextResponse,
      HasProducts = result.HasProducts,
    };
  }

}
