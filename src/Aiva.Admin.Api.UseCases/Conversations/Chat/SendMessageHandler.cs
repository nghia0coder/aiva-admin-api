namespace Aiva.Admin.Api.UseCases.Conversations.Chat;

using System.Threading;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;

public class SendMessageHandler(
    IRepository<Conversation> repository,
    IChatCompletionService chatService,
    ISystemPromptService systemPromptService,
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

    var messagesWithContext = await BuildAugmentedMessagesAsync(
            conversation,
            retrievalResult.IsSuccess ? retrievalResult.Value.FormattedContext : null,
            cancellationToken);

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
}
