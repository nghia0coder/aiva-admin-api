using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.SystemPromptAggregate;

using UserAggregate;

public class SystemPrompt : AuditableEntity<SystemPrompt, SystemPromptId>, IAggregateRoot
{
  public SystemPromptKey Key { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public string Content { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public int Version { get; private set; }
  public bool IsActive { get; private set; }

  // Required by EF Core
  private SystemPrompt() { }

  private SystemPrompt(
      SystemPromptKey key,
      string name,
      string content,
      UserId? createdBy = null,
      string? description = null)
  {
    Key = key;
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Content = Guard.Against.NullOrWhiteSpace(content, nameof(content));
    Description = description;
    Version = 1;
    IsActive = false;
  }

  public static SystemPrompt Create(
      SystemPromptKey key,
      string name,
      string content,
      UserId? createdBy = null,
      string? description = null)
  {
    var systemPrompt = new SystemPrompt(key, name, content, createdBy, description);
    return systemPrompt;
  }

  public void UpdateContent(string newContent, UserId? updatedBy = null)
  {
    Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));

    if (Content == newContent) return;

    Content = newContent;
    Version++;
  }

  public void Activate()
  {
    if (IsActive) return;

    IsActive = true;
  }

  public void Deactivate()
  {
    if (!IsActive) return;

    IsActive = false;
  }

  public void UpdateMetadata(string name, string? description, UserId? updatedBy = null)
  {
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Description = description;
  }
}
