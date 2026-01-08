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
  public TitleGenerationStatus TitleStatus { get; private set; } = TitleGenerationStatus.Pending;
  public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();
  public UserId UserId { get; private set; }

  private Conversation() { }

  public Conversation(UserId? userId, string title, string? systemPrompt = null)
  {
    Guard.Against.Null(userId, nameof(userId));

    Id = ConversationId.New();
    UserId = userId.Value;
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

  /// <summary>
  /// Check if conversation is ready for title generation
  /// (has at least one user message and one assistant response)
  /// </summary>
  public bool IsReadyForTitleGeneration()
  {
    if (TitleStatus != TitleGenerationStatus.Pending)
      return false;

    var hasUserMessage = _messages.Any(m => m.Role == ChatRole.User);
    var hasAssistantResponse = _messages.Any(m => m.Role == ChatRole.Assistant);

    return hasUserMessage && hasAssistantResponse;
  }

  public void QueueForTitleGeneration()
  {
    if (TitleStatus != TitleGenerationStatus.Pending)
      return;

    TitleStatus = TitleGenerationStatus.Queued;
    RegisterDomainEvent(new TitleGenerationQueuedEvent(this));
  }

  public void SetGeneratedTitle(string generatedTitle)
  {
    Title = Guard.Against.NullOrWhiteSpace(generatedTitle);
    TitleStatus = TitleGenerationStatus.Generated;
    RegisterDomainEvent(new TitleGeneratedEvent(this, generatedTitle));
  }

  public Conversation UpdateTitle(string newTitle)
  {
    Title = Guard.Against.NullOrWhiteSpace(newTitle);
    TitleStatus = TitleGenerationStatus.Manual;
    return this;
  }
}
