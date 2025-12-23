using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

using ConversationAggregate.Events;
using UserAggregate;

public class Conversation : EntityBase<Conversation, ConversationId>, IAggregateRoot
{
  private readonly List<ChatMessage> _messages = [];

  public string Title { get; private set; } = string.Empty;
  public string? SystemPrompt { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime? LastMessageAt { get; private set; }
  public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();
  public UserId UserId { get; private set; }

  private Conversation() { }

  public Conversation(UserId? userId, string title, string? systemPrompt = null)
  {
    Id = ConversationId.New();
    Title = Guard.Against.NullOrWhiteSpace(title);
    SystemPrompt = systemPrompt;
    CreatedAt = DateTime.UtcNow;

    // Add system prompt as first message if provided
    if (!string.IsNullOrWhiteSpace(systemPrompt))
    {
      AddMessage(ChatRole.System, systemPrompt);
    }

    RegisterDomainEvent(new ConversationCreatedEvent(this));
  }

  public ChatMessage AddMessage(ChatRole role, string content)
  {
    var message = new ChatMessage(role, content, Id);
    _messages.Add(message);
    LastMessageAt = DateTime.UtcNow;

    RegisterDomainEvent(new MessageAddedEvent(this, message));
    return message;
  }

  public Conversation UpdateTitle(string newTitle)
  {
    Title = Guard.Against.NullOrWhiteSpace(newTitle);
    return this;
  }
}
