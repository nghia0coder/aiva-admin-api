using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ShoppingChatService(
    IChatCompletionService chatService,
    IRetrievalService retrievalService,
    IRetrievalSettings retrievalSettings,
    IPromptTemplateService promptTemplateService,
    ISystemPromptService systemPromptService,
    IChatHistoryService chatHistoryService,
    IJsonExtractionService jsonExtractionService,
    IStandaloneQuestionService standaloneQuestionService,
    ILogger<ShoppingChatService> logger) : IShoppingChatService
{
  public async Task<Result<ShoppingChatResult>> ProcessShoppingChatAsync(
      Conversation conversation,
      string userMessage,
      string userName,
      CancellationToken cancellationToken)
  {
    try
    {
      // Get recent messages and chat history
      var recentMessages = conversation.GetRecentMessages();
      var chatHistory = await chatHistoryService.SerializeChatHistoryAsync(recentMessages.ToList());

      // Generate standalone question
      var standaloneMessage = promptTemplateService.ReplacePromptByKey(
          PromptTemplates.GuidelinesForShoppingStandalone,
          new ReplacePromptDto
          {
            FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
            ChatInput = userMessage,
            ChatHistory = chatHistory
          });

      var standaloneQuestionResult = await chatService.GetCompletionAsync("", standaloneMessage);

      if (string.IsNullOrWhiteSpace(standaloneQuestionResult))
      {
        logger.LogWarning("Chat service returned empty response for conversation {ConversationId}", conversation.Id);
        return Result.Error("No response received from chat service");
      }

      // Extract and parse JSON
      var jsonStandalone = jsonExtractionService.ExtractJson(standaloneQuestionResult);

      if (string.IsNullOrWhiteSpace(jsonStandalone))
      {
        logger.LogWarning("Failed to extract JSON from response for conversation {ConversationId}. Response: {Response}",
            conversation.Id, standaloneQuestionResult);
        return Result.Error("Unable to extract valid JSON from chat response");
      }

      var dataStandalone = standaloneQuestionService.ParseStandaloneQuestion(jsonStandalone);

      if (string.IsNullOrEmpty(dataStandalone.QueryString) || string.IsNullOrEmpty(dataStandalone.StandaloneQuestion))
      {
        return Result.Error("No valid query string or standalone question found in the response.");
      }

      // Perform document search
      var documentSearchResult = await retrievalService.RetrieveContextAsync(
          dataStandalone.QueryString,
          new RetrievalOptions
          {
            TopK = retrievalSettings.TopK,
            MinScore = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold,
            Strategy = SearchStrategy.Hybrid
          },
          cancellationToken);

      if (!documentSearchResult.IsSuccess)
      {
        logger.LogWarning("Document search failed for conversation {ConversationId}: {Error}",
            conversation.Id, documentSearchResult.Errors);
        return Result.Error($"Failed to retrieve context: {documentSearchResult?.Errors}");
      }

      // Get shopping assistant system prompt
      var systemPrompt = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("shopping-assistant"),
          cancellationToken);

      // Build context for shopping assistant
      var shoppingContext = $"User Query: {dataStandalone.StandaloneQuestion}\n\nRelevant Information:\n{documentSearchResult.Value.FormattedContext}";

      // Get AI response
      var responseAnswer = await chatService.GetCompletionAsync(systemPrompt, shoppingContext);

      if (string.IsNullOrWhiteSpace(responseAnswer))
      {
        logger.LogWarning("Chat service returned empty shopping response for conversation {ConversationId}", conversation.Id);
        return Result.Error("No shopping response received from chat service");
      }

      // Parse shopping response
      var result = ParseShoppingResponse(responseAnswer);

      logger.LogInformation("Successfully processed shopping chat for conversation {ConversationId}", conversation.Id);

      return Result.Success(result);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat for conversation {ConversationId}", conversation.Id);
      return Result.Error($"Shopping chat processing failed: {ex.Message}");
    }
  }

  private ShoppingChatResult ParseShoppingResponse(string responseAnswer)
  {
    var result = new ShoppingChatResult
    {
      TextResponse = responseAnswer,
      HasProducts = true
    };

    return result;
  }
}
