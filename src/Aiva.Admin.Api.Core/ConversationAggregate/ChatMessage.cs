using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

public class ChatMessage : EntityBase<ChatMessage, MessageId>
{
  public ChatRole Role { get; private set; } = null!;
  public string Content { get; private set; } = string.Empty;
  public DateTime CreatedAt { get; private set; }
  public ConversationId ConversationId { get; private set; }

  private ChatMessage() { } // EF Core

  public ChatMessage(ChatRole role, string content, ConversationId conversationId)
  {
    Id = MessageId.New();
    Role = Guard.Against.Null(role);
    Content = Guard.Against.NullOrWhiteSpace(content);
    ConversationId = conversationId;
    CreatedAt = DateTime.UtcNow;
  }
}
