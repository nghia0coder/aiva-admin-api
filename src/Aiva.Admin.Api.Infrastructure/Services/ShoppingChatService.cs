using System.Text.Json;
using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ShoppingChatService(
    IShoppingToolService shoppingToolService,
    IChatCompletionService chatService,
    IRetrievalService retrievalService,
    IRetrievalSettings retrievalSettings,
    IPromptTemplateService promptTemplateService,
    ISystemPromptService systemPromptService,
    IChatHistoryService chatHistoryService,
    IJsonExtractionService jsonExtractionService,
    IStandaloneQuestionService standaloneQuestionService,
    IHtmlTableParserService htmlTableParserService,
    ILogger<ShoppingChatService> logger) : IShoppingChatService
{
  public async Task<Result<ShoppingChatResult>> ProcessShoppingChatAsync(
      Conversation conversation,
      string userMessage,
      string userName,
      string? additionalUserData,
      CancellationToken cancellationToken)
  {
    try
    {
      // Parse additional user data if provided
      ProductSelectionData? productSelectionData = null;
      if (!string.IsNullOrWhiteSpace(additionalUserData))
      {
        productSelectionData = htmlTableParserService.ParseProductTable(additionalUserData);
        logger.LogInformation("Parsed product selection data: {SelectedCount} selected products",
            productSelectionData.SelectedProducts.Count);
      }

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
      var shoppingContext = BuildShoppingContext(
          dataStandalone.StandaloneQuestion,
          documentSearchResult.Value.FormattedContext,
          productSelectionData,
          userName);


      var availableTools = shoppingToolService.GetAvailableTools();

      var completionResult = await chatService.GetCompletionWithToolsAsync(
                                systemPrompt,
                                shoppingContext,
                                availableTools,
                                cancellationToken);

      // Get AI response
      var responseAnswer = await chatService.GetCompletionAsync(systemPrompt, shoppingContext);

      if (completionResult.HasToolCalls)
      {
        var toolResults = await ExecuteToolCallsAsync(
            completionResult.ToolCalls,
            userName,
            cancellationToken);

        // Get final response with tool results
        var finalContext = $"{shoppingContext}\n\nTool Execution Results:\n{string.Join("\n", toolResults.Values)}";
        var finalResponse = await chatService.GetCompletionAsync(systemPrompt, finalContext);

        return Result.Success(new ShoppingChatResult
        {
          TextResponse = finalResponse,
          HasProducts = true,
          ToolsExecuted = completionResult.ToolCalls.Select(tc => tc.Function.Name).ToList(),
          ToolResults = toolResults
        });
      }

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

  private async Task<Dictionary<string, object>> ExecuteToolCallsAsync(
      List<ToolCall> toolCalls,
      string userId,
      CancellationToken cancellationToken)
  {
    var results = new Dictionary<string, object>();

    foreach (var toolCall in toolCalls)
    {
      try
      {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            toolCall.Function.Arguments) ?? new Dictionary<string, object>();

        var result = await shoppingToolService.ExecuteToolAsync(
            toolCall.Function.Name,
            parameters,
            userId,
            cancellationToken);

        results[toolCall.Id] = result.IsSuccess ? result.Value : result.Errors;

        logger.LogInformation("Executed tool {FunctionName} with result: {Result}",
            toolCall.Function.Name, result.IsSuccess ? "Success" : "Failed");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to execute tool {FunctionName}", toolCall.Function.Name);
        results[toolCall.Id] = $"Tool execution failed: {ex.Message}";
      }
    }

    return results;
  }

  private string BuildShoppingContext(
      string standaloneQuestion,
      string formattedContext,
      ProductSelectionData? productSelectionData,
      string userName)
  {
    var contextBuilder = new System.Text.StringBuilder();

    contextBuilder.AppendLine($"User: {userName}");
    contextBuilder.AppendLine($"User Query: {standaloneQuestion}");
    contextBuilder.AppendLine();

    // Add product selection data if available
    if (productSelectionData?.SelectedProducts.Any() == true)
    {
      contextBuilder.AppendLine("=== User Selected Products from Table ===");
      contextBuilder.AppendLine("The user has selected the following products from the displayed table:");
      contextBuilder.AppendLine();

      foreach (var product in productSelectionData.SelectedProducts)
      {
        contextBuilder.AppendLine($"- Product: {product.ProductName}");
        contextBuilder.AppendLine($"  Product ID/URL: {product.ProductId}");
        contextBuilder.AppendLine($"  Quantity: {product.Quantity}");
        contextBuilder.AppendLine($"  Status: {(product.IsChecked ? "SELECTED (checked)" : "NOT SELECTED (unchecked)")}");

        if (product.Price.HasValue)
        {
          contextBuilder.AppendLine($"  Price: ${product.Price.Value}");
        }

        if (product.Attributes.Any())
        {
          contextBuilder.AppendLine($"  Additional Info: {string.Join(", ", product.Attributes.Select(a => $"{a.Key}={a.Value}"))}");
        }

        contextBuilder.AppendLine();
      }

      if (productSelectionData.UnselectedProducts.Any())
      {
        contextBuilder.AppendLine("Products NOT selected (unchecked):");
        contextBuilder.AppendLine(string.Join(", ", productSelectionData.UnselectedProducts));
        contextBuilder.AppendLine();
      }

      contextBuilder.AppendLine("IMPORTANT: Based on the selected products above and the user's message, call the appropriate tools:");
      contextBuilder.AppendLine("- If user wants to add to cart: call add_to_cart for each SELECTED product with their specified quantities");
      contextBuilder.AppendLine("- If user wants to remove from cart: call remove_from_cart for each UNCHECKED product");
      contextBuilder.AppendLine("- Use the exact Product ID and Quantity from the selection data above");
      contextBuilder.AppendLine();
      contextBuilder.AppendLine("=== End of Selected Products ===");
      contextBuilder.AppendLine();
    }

    contextBuilder.AppendLine("=== Relevant Product Information from Database ===");
    contextBuilder.AppendLine(formattedContext);

    return contextBuilder.ToString();
  }
}
