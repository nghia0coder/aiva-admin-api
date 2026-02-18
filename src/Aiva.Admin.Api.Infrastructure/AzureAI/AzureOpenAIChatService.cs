using System.ClientModel;
using System.Runtime.CompilerServices;
using Ardalis.Result;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace Aiva.Admin.Api.Infrastructure.AzureAI;

using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Configuration;
using Core.Interfaces;
using ChatRole = Core.ConversationAggregate.ChatRole;

public sealed class AzureOpenAIChatService : IChatCompletionService
{
  private readonly ChatClient _chatClient;
  private readonly AppSettings _appSettings;
  private readonly ILogger<AzureOpenAIChatService> _logger;

  public AzureOpenAIChatService(
      AppSettings appSettings,
      ILogger<AzureOpenAIChatService> logger)
  {
    _appSettings = appSettings;
    _logger = logger;
    _chatClient = CreateChatClient();
  }

  private ChatClient CreateChatClient()
  {
    AzureOpenAIClient azureClient;

    if (_appSettings.AzureAI.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.AzureAI.Endpoint),
          new DefaultAzureCredential());
    }
    else if (!string.IsNullOrEmpty(_appSettings.AzureAI.ApiKey))
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.AzureAI.Endpoint),
          new AzureKeyCredential(_appSettings.AzureAI.ApiKey));
    }
    else
    {
      throw new InvalidOperationException(
          "Azure AI configuration is invalid. " +
          "Provide either ApiKey or enable UseManagedIdentity.");
    }

    return azureClient.GetChatClient(_appSettings.AzureAI.DeploymentName);
  }

  public async Task<Result<string>> GetCompletionAsync(
      IReadOnlyList<Core.ConversationAggregate.ChatMessage> messages,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var chatMessages = MapToChatMessages(messages);

      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = _appSettings.AzureAI.MaxTokens,
        Temperature = _appSettings.AzureAI.Temperature
      };

      ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(
          chatMessages,
          options,
          cancellationToken);

      var content = response.Value.Content[0].Text;

      _logger.LogInformation(
          "Chat completion successful. Tokens used: {TotalTokens}",
          response.Value.Usage.TotalTokenCount);

      return Result.Success(content);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during chat completion");
      return Result.Error($"AI service error: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during chat completion");
      return Result.Error("An unexpected error occurred while processing your request.");
    }
  }

  public async IAsyncEnumerable<Result<string>> StreamCompletionAsync(
      IReadOnlyList<Core.ConversationAggregate.ChatMessage> messages,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var chatMessages = MapToChatMessages(messages);

    var options = new ChatCompletionOptions
    {
      MaxOutputTokenCount = _appSettings.AzureAI.MaxTokens,
      Temperature = _appSettings.AzureAI.Temperature
    };

    AsyncCollectionResult<StreamingChatCompletionUpdate> streamingUpdates;
    string? errorMessage = null;

    try
    {
      streamingUpdates = _chatClient.CompleteChatStreamingAsync(
          chatMessages,
          options,
          cancellationToken);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during streaming");
      yield break;
    }

    if (errorMessage is not null)
    {
      yield return Result.Error(errorMessage);
      yield break;
    }

    await foreach (var update in streamingUpdates.WithCancellation(cancellationToken))
    {
      foreach (var contentPart in update.ContentUpdate)
      {
        if (!string.IsNullOrEmpty(contentPart.Text))
        {
          yield return Result.Success(contentPart.Text);
        }
      }
    }
  }

  private List<OpenAI.Chat.ChatMessage> MapToChatMessages(IReadOnlyList<Core.ConversationAggregate.ChatMessage> messages)
  {
    return messages.Select<Core.ConversationAggregate.ChatMessage, OpenAI.Chat.ChatMessage>(m =>
    {
      return m.Role.Name switch
      {
        nameof(ChatRole.System) => new SystemChatMessage(m.Content),
        nameof(ChatRole.User) => new UserChatMessage(m.Content),
        nameof(ChatRole.Assistant) => new AssistantChatMessage(m.Content),
        _ => throw new ArgumentException($"Unknown chat role: {m.Role.Name}")
      };
    }).ToList();
  }

  public async Task<Result<string>> GetCompletionAsync(string system, string message, CancellationToken cancellationToken = default)
  {
    try
    {
      var messages = new List<ChatMessage>();
      if (!string.IsNullOrEmpty(system))
      {
        messages.Add(new SystemChatMessage(system));
      }
      if (!string.IsNullOrEmpty(message))
      {
        messages.Add(new UserChatMessage(message));
      }

      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = _appSettings.AzureAI.MaxTokens,
        Temperature = _appSettings.AzureAI.Temperature
      };

      ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

      var content = response.Value.Content[0].Text;

      _logger.LogInformation(
          "Chat completion successful. Tokens used: {TotalTokens}",
          response.Value.Usage.TotalTokenCount);

      return Result.Success(content);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during chat completion");
      return Result.Error($"AI service error: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during chat completion");
      return Result.Error("An unexpected error occurred while processing your request.");
    }
  }

  public async Task<ChatCompletionResult> GetCompletionWithToolsAsync(
    string systemPrompt,
    string userPrompt,
    IEnumerable<ToolDefinition> tools,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var messages = new List<ChatMessage>();

      if (!string.IsNullOrEmpty(systemPrompt))
      {
        messages.Add(new SystemChatMessage(systemPrompt));
      }

      if (!string.IsNullOrEmpty(userPrompt))
      {
        messages.Add(new UserChatMessage(userPrompt));
      }

      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = _appSettings.AzureAI.MaxTokens,
        Temperature = _appSettings.AzureAI.Temperature,
        ToolChoice = ChatToolChoice.CreateAutoChoice()
      };

      // Convert ToolDefinitions to OpenAI ChatTool format
      foreach (var toolDef in tools)
      {
        var chatTool = CreateChatToolFromDefinition(toolDef);
        options.Tools.Add(chatTool);
      }

      _logger.LogDebug("Calling Azure OpenAI with {ToolCount} tools available", tools.Count());

      var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

      var result = new ChatCompletionResult
      {
        FinishReason = response.Value.FinishReason.ToString()
      };

      // Check if the response contains tool calls
      if (response.Value.ToolCalls?.Any() == true)
      {
        result.ToolCalls = response.Value.ToolCalls
            .Select(MapToToolCall)
            .ToList();

        _logger.LogInformation("Received {ToolCallCount} tool calls from LLM", result.ToolCalls.Count);
      }
      else
      {
        // Regular text response
        result.TextResponse = response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        _logger.LogInformation("Received text response from LLM (no tools called)");
      }

      _logger.LogInformation(
          "Chat completion with tools successful. Tokens used: {TotalTokens}",
          response.Value.Usage.TotalTokenCount);

      return result;
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during chat completion with tools");
      throw new InvalidOperationException($"AI service error: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during chat completion with tools");
      throw;
    }
  }

  private ChatTool CreateChatToolFromDefinition(ToolDefinition toolDefinition)
  {
    // Convert our ToolDefinition to OpenAI's ChatToolFunction
    var functionDef = toolDefinition.Function;

    // Serialize parameters to JSON string as required by OpenAI SDK
    var parametersJson = JsonSerializer.Serialize(functionDef.Parameters);

    var chatFunction = ChatTool.CreateFunctionTool(
        functionName: functionDef.Name,
        functionDescription: functionDef.Description,
        functionParameters: BinaryData.FromString(parametersJson)
    );

    _logger.LogDebug("Created chat tool: {ToolName} - {Description}",
        functionDef.Name, functionDef.Description);

    return chatFunction;
  }

  private ToolCall MapToToolCall(ChatToolCall openAIToolCall)
  {
    return new ToolCall
    {
      Id = openAIToolCall.Id,
      Type = "function",
      Function = new FunctionCall
      {
        Name = openAIToolCall.FunctionName,
        Arguments = openAIToolCall.FunctionArguments.ToString() // Assuming arguments are returned as JSON string
      }
    };
  }
}
