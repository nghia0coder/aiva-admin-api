namespace Aiva.Admin.Api.UseCases.Conversations.Chat;

using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;

public class SendMessageHandler(
    IRepository<Conversation> repository,
    IChatCompletionService chatService,
    IRetrievalService retrievalService)
    : ICommandHandler<SendMessageCommand, Result<ChatMessageDTO>>
{
  public async ValueTask<Result<ChatMessageDTO>> Handle(
      SendMessageCommand command,
      CancellationToken cancellationToken)
  {
    var spec = new ConversationByIdWithMessagesSpec(command.ConversationId);
    var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound("Conversation not found.");
    }

    // Add user message
    conversation.AddMessage(ChatRole.User, command.UserMessage);

    // *** RAG: Retrieve relevant context ***
    var retrievalResult = await retrievalService.RetrieveContextAsync(
            command.UserMessage,
            new RetrievalOptions
            {
              TopK = 5,
              MinScore = 0.7,
              Strategy = SearchStrategy.Hybrid  // Use hybrid for e-commerce
            },
            cancellationToken);

    var messagesWithContext = BuildAugmentedMessages(
            conversation.Messages,
            retrievalResult.IsSuccess ? retrievalResult.Value.FormattedContext : null, conversation.Id);

    // Get AI response
    var completionResult = await chatService.GetCompletionAsync(
        messagesWithContext,
        cancellationToken);

    if (!completionResult.IsSuccess)
    {
      return Result.Error(completionResult.Errors.ToString());
    }

    // Add assistant response
    var assistantMessage = conversation.AddMessage(
        ChatRole.Assistant,
        completionResult.Value);

    if (conversation.IsReadyForTitleGeneration())
    {
      conversation.QueueForTitleGeneration();
    }

    await repository.UpdateAsync(conversation, cancellationToken);

    return Result.Success(new ChatMessageDTO(
        assistantMessage.Id.Value,
        assistantMessage.Role.Name,
        assistantMessage.Content,
        assistantMessage.CreatedAt));
  }

  private IReadOnlyList<ChatMessage> BuildAugmentedMessages(
        IReadOnlyList<ChatMessage> messages,
        string? context, ConversationId conversationId)
  {
    if (string.IsNullOrEmpty(context))
      return messages;

    var augmented = messages.ToList();

    // Inject context before the last user message
    var lastUserIndex = augmented.FindLastIndex(m => m.Role == ChatRole.User);
    if (lastUserIndex >= 0)
    {
      var originalContent = augmented[lastUserIndex].Content;
      augmented[lastUserIndex] = new ChatMessage(
          ChatRole.User,
          $"{context}\n\nQuestion: {originalContent}", conversationId);
    }

    return augmented;
  }
}
