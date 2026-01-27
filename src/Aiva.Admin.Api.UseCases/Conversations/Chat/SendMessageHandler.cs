namespace Aiva.Admin.Api.UseCases.Conversations.Chat;

using System.Threading;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

public class SendMessageHandler(
    IRepository<Conversation> repository,
    IChatCompletionService chatService,
    ISystemPromptService systemPromptService,
    IRetrievalService retrievalService,
    IRetrievalSettings retrievalSettings,
    ILogger<SendMessageHandler> logger)
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
    // Use hybrid-specific threshold if available, otherwise fall back to default threshold
    var searchStrategy = SearchStrategy.Hybrid;
    var minScoreForSearch = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold;
    var minScoreForValidation = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold;
    
    var retrievalResult = await retrievalService.RetrieveContextAsync(
            command.UserMessage,
            new RetrievalOptions
            {
              TopK = retrievalSettings.TopK,
              MinScore = minScoreForSearch,
              Strategy = searchStrategy  // Use hybrid for e-commerce
            },
            cancellationToken);

    // *** GATE CHECK: Out-of-Scope Detection ***
    if (retrievalSettings.EnableOutOfScopeDetection &&
        (!retrievalResult.IsSuccess ||
         !retrievalResult.Value.HasSufficientContext(
             minScoreForValidation,
             retrievalSettings.MinResultCount)))
    {
      // No relevant context found - return standard out-of-scope response without calling LLM
      var outOfScopeMessage = OutOfScopeResponse.Default;

      logger.LogWarning(
          "Out-of-scope query detected. Query: {Query}, TopScore: {TopScore:F4}, ResultCount: {Count}, MinScoreThreshold: {MinScore}",
          command.UserMessage,
          retrievalResult.IsSuccess ? retrievalResult.Value.TopScore : 0,
          retrievalResult.IsSuccess ? retrievalResult.Value.Results.Count : 0,
          minScoreForValidation);

      // Add out-of-scope response to conversation
      var outOfScopeResponse = conversation.AddMessage(ChatRole.Assistant, outOfScopeMessage);
      await repository.UpdateAsync(conversation, cancellationToken);

      return Result.Success(new ChatMessageDTO(
          outOfScopeResponse.Id.Value,
          outOfScopeResponse.Role.Name,
          outOfScopeResponse.Content,
          outOfScopeResponse.CreatedAt));
    }
    // *** END GATE CHECK ***

    var messagesWithContext = await BuildAugmentedMessagesAsync(
            conversation,
            retrievalResult.Value.FormattedContext,
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
