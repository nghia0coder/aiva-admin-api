using System.Text.Json;
using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamShoppingChatHandler(
IRepository<Conversation> repository,
IChatCompletionService chatService,
IRetrievalService retrievalService,
IRetrievalSettings retrievalSettings,
IPromptTemplateService promptTemplateService,
ISystemPromptService systemPromptService,
IChatHistoryService chatHistoryService,
ILogger<StreamShoppingChatHandler> logger)
: IRequestHandler<StreamShoppingChatCommand, Result<StreamShoppingChatResponse>>
{
  public async ValueTask<Result<StreamShoppingChatResponse>> Handle(StreamShoppingChatCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(request.ConversationId));
      var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

      if (conversation is null)
      {
        return Result.NotFound("Conversation not found");
      }

      var recentMessages = conversation.GetRecentMessages();
      var chatHistory = await chatHistoryService.SerializeChatHistoryAsync(recentMessages.ToList());

      conversation.AddMessage(ChatRole.User, request.Message);

      var standaloneMessage = promptTemplateService.ReplacePromptByKey(
          PromptTemplates.GuidelinesForShoppingStandalone,
          new ReplacePromptDto
          {
            FullName = "Nghia Dai", // TODO: Replace with actual user data
            ChatInput = request.Message,
            ChatHistory = chatHistory
          });

      var standaloneQuestionResult = await chatService.GetCompletionAsync("", standaloneMessage);

      if (string.IsNullOrWhiteSpace(standaloneQuestionResult))
      {
        logger.LogWarning("Chat service returned empty response for conversation {ConversationId}", request.ConversationId);
        return Result.Error("No response received from chat service");
      }

      var jsonStandalone = GetSubstringJson(standaloneQuestionResult);

      if (string.IsNullOrWhiteSpace(jsonStandalone))
      {
        logger.LogWarning("Failed to extract JSON from response for conversation {ConversationId}. Response: {Response}", 
          request.ConversationId, standaloneQuestionResult);
        return Result.Error("Unable to extract valid JSON from chat response");
      }

      var dataStandalone = GetDataStandalone(jsonStandalone);

      var standaloneQuestion = dataStandalone.StandaloneQuestion;
      var standaloneJson = standaloneQuestionResult;
      var documentContent = string.Empty;

      if (!string.IsNullOrEmpty(dataStandalone.QueryString))
      {
        var keyWords = dataStandalone.QueryString.Split(";").Where(t => !string.IsNullOrEmpty(t)).ToList();
        var documentSearchResult = await retrievalService.RetrieveContextAsync(standaloneQuestionResult, new RetrievalOptions
        {
          TopK = retrievalSettings.TopK,
          MinScore = retrievalSettings.HybridSearchMinScoreThreshold ?? retrievalSettings.MinScoreThreshold,
          Strategy = SearchStrategy.Hybrid
        }, cancellationToken);

        // TODO: Get shopping assistant system prompt
        var systemPrompt = await systemPromptService.GetActivePromptContentAsync(
            SystemPromptKey.From("shopping-assistant"),
            cancellationToken);

        // Build context for shopping assistant with user query and document context
        var shoppingContext = $"User Query: {standaloneQuestion}\n\nRelevant Information:\n{documentSearchResult.Value.FormattedContext}";

        var responseAnswer = await chatService.GetCompletionAsync(systemPrompt, shoppingContext);

        var response = new StreamShoppingChatResponse
        {
          TextResponse = responseAnswer,
          HasProducts = false
        };

        // TODO: Implement shopping-specific response parsing:
        // Parse responseAnswer to extract:
        // 1. Product recommendations (if AI suggests specific products)
        // 2. Price comparison data (if AI provides pricing info)
        // 3. Shopping advice (if AI gives specific advice)
        // Example parsing logic:
        // if (responseAnswer.Contains("recommend") || responseAnswer.Contains("product"))
        // {
        //     response.HasProducts = true;
        //     response.ProductRecommendations = ParseProductRecommendations(responseAnswer);
        // }
        // if (responseAnswer.Contains("price") || responseAnswer.Contains("cost"))
        // {
        //     response.PriceComparison = ParsePriceComparison(responseAnswer);
        // }
        // response.ShoppingAdvice = ExtractAdvice(responseAnswer);

        // Save assistant message
        conversation.AddMessage(ChatRole.Assistant, response.TextResponse);

        if (conversation.IsReadyForTitleGeneration())
        {
          conversation.QueueForTitleGeneration();
        }

        await repository.UpdateAsync(conversation, cancellationToken);

        return Result.Success(response);
      }

      return Result.Error("No keywords found in the standalone data.");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat stream for conversation {ConversationId}", request.ConversationId);
      return Result.Error(ex.Message);
    }
  }

  public class ResponseStandaloneDto
  {
    public string? QueryString { get; set; }
    public List<string>? KeyWords { get; set; }
    public string? StandaloneQuestion { get; set; }
    public List<string>? SummaryColumn { get; set; }
  }

  public ResponseStandaloneDto GetDataStandalone(string jsonStandalone)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(jsonStandalone))
      {
        throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonStandalone));
      }

      // Log the JSON being parsed for debugging
      logger.LogDebug("Parsing JSON: {JsonContent}", jsonStandalone);

      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
      };

      var dataStandalone = JsonSerializer.Deserialize<ResponseStandaloneDto>(jsonStandalone, options);

      if (dataStandalone == null)
      {
        throw new InvalidOperationException($"Failed to deserialize JSON to ResponseStandaloneDto. JSON content: {jsonStandalone}");
      }

      // Initialize KeyWords list
      dataStandalone.KeyWords = new List<string>();

      if (!string.IsNullOrEmpty(dataStandalone.QueryString))
      {
        dataStandalone.KeyWords = dataStandalone.QueryString
          .Split(';')
          .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
          .Select(keyword => keyword.Trim())
          .ToList();
      }

      return dataStandalone;
    }
    catch (JsonException jsonEx)
    {
      logger.LogError(jsonEx, "JSON deserialization failed. Input: {JsonContent}", jsonStandalone);
      throw new Exception($"Invalid JSON format: {jsonEx.Message}. Input: {jsonStandalone}");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "GetDataStandalone failed. Input: {JsonContent}", jsonStandalone);
      throw new Exception($"GetDataStandalone failed: {ex.Message}. Input: {jsonStandalone}");
    }
  }

  public static string GetSubstringJson(string text)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        throw new ArgumentException("Input text cannot be null or empty");
      }

      // Try to find JSON wrapped in code blocks
      var jsonStart = "```json";
      var codeBlockEnd = "```";

      var jsonStartIndex = text.IndexOf(jsonStart);
      if (jsonStartIndex >= 0)
      {
        // Found ```json, look for the JSON content
        var contentStart = jsonStartIndex + jsonStart.Length;
        var remainingText = text.Substring(contentStart);

        var endIndex = remainingText.IndexOf(codeBlockEnd);
        if (endIndex >= 0)
        {
          // Found closing ```, extract the JSON
          var jsonContent = remainingText.Substring(0, endIndex);
          return CleanJsonString(jsonContent);
        }
        else
        {
          // No closing ```, take everything after ```json
          return CleanJsonString(remainingText);
        }
      }

      // Try to find JSON wrapped in generic code blocks
      var genericStart = "```";
      var genericStartIndex = text.IndexOf(genericStart);
      if (genericStartIndex >= 0)
      {
        var contentStart = genericStartIndex + genericStart.Length;
        var remainingText = text.Substring(contentStart);

        var endIndex = remainingText.IndexOf(codeBlockEnd);
        if (endIndex >= 0)
        {
          var jsonContent = remainingText.Substring(0, endIndex);
          return CleanJsonString(jsonContent);
        }
        else
        {
          return CleanJsonString(remainingText);
        }
      }

      // No code blocks found, try to extract JSON directly
      // Look for opening brace
      var openBraceIndex = text.IndexOf('{');
      if (openBraceIndex >= 0)
      {
        // Find the matching closing brace
        var braceCount = 0;
        var closeBraceIndex = -1;

        for (int i = openBraceIndex; i < text.Length; i++)
        {
          if (text[i] == '{') braceCount++;
          else if (text[i] == '}') braceCount--;

          if (braceCount == 0)
          {
            closeBraceIndex = i;
            break;
          }
        }

        if (closeBraceIndex >= 0)
        {
          var jsonContent = text.Substring(openBraceIndex, closeBraceIndex - openBraceIndex + 1);
          return CleanJsonString(jsonContent);
        }
      }

      // If no JSON structure found, return the original text cleaned
      return CleanJsonString(text);
    }
    catch (Exception ex)
    {
      throw new Exception("Failed to extract JSON substring: " + ex.Message);
    }
  }

  private static string CleanJsonString(string json)
  {
    return json
      .Replace("\n", " ")
      .Replace("\r", " ")
      .Replace("\t", " ")
      .Trim();
  }
}
