namespace Aiva.Admin.Api.Core.Interfaces;

using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using ConversationAggregate;

/// <summary>
/// Service for AI chat completions using Azure AI Foundry / Azure OpenAI
/// </summary>
public interface IChatCompletionService
{
  /// <summary>
  /// Sends a conversation and gets a complete AI response
  /// </summary>
  /// <param name="messages">The conversation history</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The AI assistant's response</returns>
  Task<Result<string>> GetCompletionAsync(
      IReadOnlyList<ChatMessage> messages,
      CancellationToken cancellationToken = default);


  /// <summary>
  /// Sends a conversation and gets a complete AI response
  /// </summary>
  /// <param name="messages">The conversation history</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The AI assistant's response</returns>
  Task<Result<string>> GetCompletionAsync(
      string system,
      string messages,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Streams the AI response as it's generated (for real-time UI updates)
  /// </summary>
  /// <param name="messages">The conversation history</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Async stream of response chunks</returns>
  IAsyncEnumerable<Result<string>> StreamCompletionAsync(
      IReadOnlyList<ChatMessage> messages,
      CancellationToken cancellationToken = default);

  Task<ChatCompletionResult> GetCompletionWithToolsAsync(
        string systemPrompt,
        string userPrompt,
        IEnumerable<ToolDefinition> tools,
        CancellationToken cancellationToken = default);

  /// <summary>
  /// Selects tools that the model needs to execute without generating text completion
  /// </summary>
  /// <param name="systemPrompt">System instructions for the AI</param>
  /// <param name="userPrompt">User input</param>
  /// <param name="tools">Available tools the model can select from</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>List of tools that the model wants to execute</returns>
  Task<Result<List<ToolCall>>> SelectToolsAsync(
      string systemPrompt,
      string userPrompt,
      IEnumerable<ToolDefinition> tools,
      CancellationToken cancellationToken = default);
}
