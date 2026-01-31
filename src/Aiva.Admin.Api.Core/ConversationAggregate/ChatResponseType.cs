using Ardalis.SmartEnum;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

/// <summary>
/// Defines the type of response content returned by the chatbot.
/// Determines how the frontend should render the response.
/// </summary>
public sealed class ChatResponseType : SmartEnum<ChatResponseType>
{
  /// <summary>
  /// Plain text response (default)
  /// </summary>
  public static readonly ChatResponseType Text = new(nameof(Text), 0);

  /// <summary>
  /// Structured table data response (e.g., product listings, comparisons)
  /// </summary>
  public static readonly ChatResponseType StructuredTable = new(nameof(StructuredTable), 1);

  /// <summary>
  /// Mixed response containing both text and structured data
  /// </summary>
  public static readonly ChatResponseType Mixed = new(nameof(Mixed), 2);

  private ChatResponseType(string name, int value) : base(name, value)
  {
  }
}
