using System.ClientModel;
using System.Runtime.CompilerServices;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

namespace Aiva.Admin.Api.Infrastructure.AzureAI;

using Aiva.Admin.Api.Core.ConversationAggregate;
using Ardalis.Result;
using Core.Interfaces;
using OpenAI.Chat;

public sealed class AzureOpenAIChatService : IChatCompletionService
{
  private readonly ChatClient _chatClient;
  private readonly AzureAIConfiguration _configuration;
  private readonly ILogger<AzureOpenAIChatService> _logger;

  public AzureOpenAIChatService(
      IOptions<AzureAIConfiguration> options,
      ILogger<AzureOpenAIChatService> logger)
  {
    _configuration = options.Value;
    _logger = logger;
    _chatClient = CreateChatClient();
  }

  private ChatClient CreateChatClient()
  {
    AzureOpenAIClient azureClient;

    if (_configuration.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_configuration.Endpoint),
          new DefaultAzureCredential());
    }
    else if (!string.IsNullOrEmpty(_configuration.ApiKey))
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_configuration.Endpoint),
          new AzureKeyCredential(_configuration.ApiKey));
    }
    else
    {
      throw new InvalidOperationException(
          "Azure AI configuration is invalid. " +
          "Provide either ApiKey or enable UseManagedIdentity.");
    }

    return azureClient.GetChatClient(_configuration.DeploymentName);
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
        MaxOutputTokenCount = _configuration.MaxTokens,
        Temperature = _configuration.Temperature
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
      MaxOutputTokenCount = _configuration.MaxTokens,
      Temperature = _configuration.Temperature
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
}
