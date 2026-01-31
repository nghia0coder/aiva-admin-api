using System.ClientModel;
using System.Text.Json;
using Ardalis.Result;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Aiva.Admin.Api.Infrastructure.AzureAI;

using Core.ConversationAggregate;
using Core.Interfaces;
using Infrastructure.Configuration;

/// <summary>
/// Azure OpenAI implementation of intent detection service
/// Uses structured output (JSON mode) for reliable intent classification
/// </summary>
public sealed class AzureOpenAIIntentDetectionService : IIntentDetectionService
{
  private readonly ChatClient _chatClient;
  private readonly IntentDetectionSettings _settings;
  private readonly ILogger<AzureOpenAIIntentDetectionService> _logger;
  private readonly string _systemPrompt;

  public AzureOpenAIIntentDetectionService(
      AzureAISettings aiSettings,
      IntentDetectionSettings intentSettings,
      ILogger<AzureOpenAIIntentDetectionService> logger)
  {
    _settings = intentSettings;
    _logger = logger;

    // Initialize Azure OpenAI client
    AzureOpenAIClient azureClient;
    
    if (aiSettings.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(new Uri(aiSettings.Endpoint), new DefaultAzureCredential());
    }
    else
    {
      azureClient = new AzureOpenAIClient(new Uri(aiSettings.Endpoint), new AzureKeyCredential(aiSettings.ApiKey));
    }

    _chatClient = azureClient.GetChatClient(_settings.ModelDeployment);

    // Load system prompt from file
    _systemPrompt = LoadSystemPrompt(_settings.PromptFilePath);
  }

  public async Task<Result<IntentDetectionResult>> DetectIntentAsync(
      string query,
      IReadOnlyList<ChatMessage> conversationHistory,
      CancellationToken cancellationToken = default)
  {
    if (!_settings.Enabled)
    {
      // Intent detection disabled, return default General intent
      return Result.Success(new IntentDetectionResult(
          QueryIntent.General,
          Confidence: 1.0,
          RequiresStructuredResponse: false)
      {
        Reasoning = "Intent detection is disabled"
      });
    }

    try
    {
      var chatMessages = BuildChatMessages(query, conversationHistory);

      var options = new ChatCompletionOptions
      {
        Temperature = (float)_settings.Temperature,
        MaxOutputTokenCount = _settings.MaxTokens,
        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()  // JSON mode for structured output
      };

      _logger.LogInformation(
          "Detecting intent for query: {Query} (History messages: {HistoryCount})",
          query.Length > 100 ? query.Substring(0, 100) + "..." : query,
          conversationHistory.Count);

      ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(
          chatMessages,
          options,
          cancellationToken);

      var content = response.Value.Content[0].Text;

      _logger.LogDebug("Intent detection raw response: {Response}", content);

      // Parse JSON response
      var intentResponse = JsonSerializer.Deserialize<IntentDetectionJsonResponse>(content, new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      });

      if (intentResponse == null)
      {
        _logger.LogWarning("Failed to parse intent detection response");
        return Result.Error("Failed to parse intent detection response");
      }

      // Map JSON intent string to QueryIntent enum
      var intent = MapIntentString(intentResponse.Intent);
      var confidence = intentResponse.Confidence;

      // Check confidence threshold
      if (confidence < _settings.ConfidenceThreshold && intent != QueryIntent.General)
      {
        _logger.LogInformation(
            "Intent confidence ({Confidence:F2}) below threshold ({Threshold:F2}), defaulting to General",
            confidence,
            _settings.ConfidenceThreshold);

        intent = QueryIntent.General;
      }

      // Extract entities if enabled
      var entities = _settings.EnableEntityExtraction
          ? ExtractEntities(intentResponse.ExtractedEntities)
          : null;

      var result = new IntentDetectionResult(
          intent,
          confidence,
          intentResponse.RequiresStructuredResponse,
          entities)
      {
        Reasoning = intentResponse.Reasoning
      };

      _logger.LogInformation(
          "Intent detected: {Intent} (Confidence: {Confidence:F2}, RequiresStructured: {RequiresStructured})",
          intent.Name,
          confidence,
          result.RequiresStructuredResponse);

      return Result.Success(result);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI request failed during intent detection");
      return Result.Error($"Intent detection failed: {ex.Message}");
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Failed to deserialize intent detection response");
      return Result.Error($"Invalid intent detection response format: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during intent detection");
      return Result.Error($"Intent detection error: {ex.Message}");
    }
  }

  private List<OpenAI.Chat.ChatMessage> BuildChatMessages(string query, IReadOnlyList<ChatMessage> conversationHistory)
  {
    var messages = new List<OpenAI.Chat.ChatMessage>
    {
      new SystemChatMessage(_systemPrompt)
    };

    // Build context from conversation history (last 5 messages for context)
    var historyContext = conversationHistory
        .TakeLast(5)
        .Select(m => $"{m.Role.Name}: {m.Content}")
        .ToList();

    var userPrompt = historyContext.Count > 0
        ? $"Conversation history:\n{string.Join("\n", historyContext)}\n\nCurrent user query: {query}"
        : $"Current user query: {query}";

    messages.Add(new UserChatMessage(userPrompt));

    return messages;
  }

  private QueryIntent MapIntentString(string intentString)
  {
    return intentString.ToLowerInvariant() switch
    {
      "browse" => QueryIntent.Browse,
      "compare" => QueryIntent.Compare,
      "purchase" => QueryIntent.Purchase,
      "order_tracking" => QueryIntent.OrderTracking,
      "ordertracking" => QueryIntent.OrderTracking,
      _ => QueryIntent.General
    };
  }

  private IReadOnlyDictionary<string, string>? ExtractEntities(Dictionary<string, object?>? rawEntities)
  {
    if (rawEntities == null || rawEntities.Count == 0)
      return null;

    var entities = new Dictionary<string, string>();

    foreach (var kvp in rawEntities)
    {
      if (kvp.Value != null)
      {
        // Handle nested objects (like priceRange)
        if (kvp.Value is JsonElement jsonElement)
        {
          entities[kvp.Key] = jsonElement.ToString();
        }
        else
        {
          entities[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
        }
      }
    }

    return entities.Count > 0 ? entities : null;
  }

  private string LoadSystemPrompt(string filePath)
  {
    try
    {
      var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

      if (!File.Exists(fullPath))
      {
        _logger.LogWarning("Intent detection prompt file not found at {Path}, using fallback", fullPath);
        return GetFallbackSystemPrompt();
      }

      var content = File.ReadAllText(fullPath);
      _logger.LogInformation("Loaded intent detection prompt from {Path}", fullPath);
      return content;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load intent detection prompt from {Path}", filePath);
      return GetFallbackSystemPrompt();
    }
  }

  private static string GetFallbackSystemPrompt()
  {
    return @"You are an intent classification expert for an e-commerce chatbot.
Classify user queries into one of these intents: general, browse, compare, purchase, order_tracking.
Return ONLY valid JSON in this format:
{
  ""intent"": ""browse"",
  ""confidence"": 0.85,
  ""requiresStructuredResponse"": true,
  ""extractedEntities"": {},
  ""reasoning"": ""User wants to browse products""
}";
  }

  /// <summary>
  /// JSON response format from intent detection
  /// </summary>
  private sealed record IntentDetectionJsonResponse(
      string Intent,
      double Confidence,
      bool RequiresStructuredResponse,
      Dictionary<string, object?>? ExtractedEntities,
      string? Reasoning);
}
