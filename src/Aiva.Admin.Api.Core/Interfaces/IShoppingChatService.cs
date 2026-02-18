using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IJsonExtractionService
{
  /// <summary>
  /// Extracts JSON content from text that may be wrapped in markdown code blocks
  /// </summary>
  /// <param name="text">The text containing JSON</param>
  /// <returns>Extracted and cleaned JSON string</returns>
  string ExtractJson(string text);
}

public interface IStandaloneQuestionService
{
  /// <summary>
  /// Parses JSON string into StandaloneQuestionDto and enriches it with keywords
  /// </summary>
  /// <param name="jsonContent">JSON string to parse</param>
  /// <returns>Parsed and enriched standalone question data</returns>
  StandaloneQuestionDto ParseStandaloneQuestion(string jsonContent);
}

public interface IShoppingChatService
{
  /// <summary>
  /// Processes a shopping chat request and generates appropriate response
  /// </summary>
  /// <param name="conversation">The conversation to process</param>
  /// <param name="userMessage">User's message</param>
  /// <param name="userName">User's full name</param>
  /// <param name="additionalUserData">Optional HTML table data from frontend with product selections</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Shopping chat response</returns>
  Task<Result<ShoppingChatResult>> ProcessShoppingChatAsync(
      ConversationAggregate.Conversation conversation,
      string userMessage,
      string userName,
      string? additionalUserData,
      CancellationToken cancellationToken);
}

public class ShoppingChatResult
{
  public string TextResponse { get; set; } = string.Empty;
  public bool HasProducts { get; set; }
  public List<string> ToolsExecuted { get; set; } = new();
  public Dictionary<string, object> ToolResults { get; set; } = new();
}
