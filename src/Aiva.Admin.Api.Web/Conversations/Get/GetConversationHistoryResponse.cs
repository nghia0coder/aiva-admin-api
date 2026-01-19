namespace Aiva.Admin.Api.Web.Conversations.Get;

public record GetConversationHistoryResponse(
    Guid Id,
    string Title,
    string? SystemPrompt,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageRecord> Messages,
    ConversationMetadata Metadata,
    PaginationInfo? Pagination = null);

public record ChatMessageRecord(
    Guid Id,
    string Role,           // "system", "user", "assistant" - OpenAI standard
    string Content,
    DateTime CreatedAt,
    MessageMetadata? Metadata = null)
{
  // Computed properties for FE convenience
  public bool IsUser => Role == "user";
  public bool IsAssistant => Role == "assistant";
  public bool IsSystem => Role == "system";
  public string DisplayRole => Role.ToTitleCase();
  public string TimeAgo => FormatTimeAgo(CreatedAt);

  private static string FormatTimeAgo(DateTime createdAt)
  {
    var timeSpan = DateTime.UtcNow - createdAt;
    return timeSpan.TotalMinutes switch
    {
      < 1 => "Just now",
      < 60 => $"{(int)timeSpan.TotalMinutes}m ago",
      < 1440 => $"{(int)timeSpan.TotalHours}h ago",
      < 10080 => $"{(int)timeSpan.TotalDays}d ago",
      _ => createdAt.ToString("MMM dd, yyyy")
    };
  }
}

public record MessageMetadata(
    int TokenCount,
    TimeSpan? ResponseTime = null,
    string? Model = null,
    bool IsEdited = false,
    DateTime? EditedAt = null,
    MessageStatus Status = MessageStatus.Completed)
{
  // FE-friendly computed properties
  public string TokenCountDisplay => TokenCount > 1000 ? $"{TokenCount / 1000.0:F1}k" : TokenCount.ToString();
  public string? ResponseTimeDisplay => ResponseTime?.TotalSeconds > 0 ? $"{ResponseTime.Value.TotalSeconds:F1}s" : null;
}

public record ConversationMetadata(
    int TotalMessages,
    int TotalTokens,
    DateTime LastActiveAt,
    ConversationStatus Status = ConversationStatus.Active,
    bool IsArchived = false)
{
  // FE-friendly computed properties  
  public string TotalTokensDisplay => TotalTokens > 1000 ? $"{TotalTokens / 1000.0:F1}k" : TotalTokens.ToString();
  public bool HasMessages => TotalMessages > 0;
  public string LastActiveDisplay => FormatLastActive(LastActiveAt);

  private static string FormatLastActive(DateTime lastActive)
  {
    var timeSpan = DateTime.UtcNow - lastActive;
    return timeSpan.TotalMinutes switch
    {
      < 1 => "Active now",
      < 60 => $"Active {(int)timeSpan.TotalMinutes}m ago",
      < 1440 => $"Active {(int)timeSpan.TotalHours}h ago",
      _ => $"Last active {lastActive:MMM dd}"
    };
  }
}

// Enums for consistent status handling
public enum MessageStatus
{
  Pending,
  Streaming,
  Completed,
  Failed,
  Filtered
}

public enum ConversationStatus
{
  Active,
  Archived,
  Deleted
}

// Pagination information for cursor-based pagination
public record PaginationInfo(
    bool HasMore,              // More messages exist before oldest returned
    bool HasNewer,             // More messages exist after newest returned
    Guid? OldestMessageId,     // Cursor to fetch previous page
    Guid? NewestMessageId,     // Cursor to fetch next page
    DateTime? OldestTimestamp, // Alternative timestamp-based cursor
    DateTime? NewestTimestamp, // Alternative timestamp-based cursor
    int TotalMessages,         // Total messages in conversation (for UI)
    int ReturnedCount)         // Messages in this response
{
    // FE-friendly computed properties
    public bool CanLoadMore => HasMore;
    public bool CanLoadNewer => HasNewer;
    public double LoadedPercentage => TotalMessages > 0 
        ? Math.Round((double)ReturnedCount / TotalMessages * 100, 1) 
        : 100.0;
}

// Extension method for string formatting
public static class StringExtensions
{
  public static string ToTitleCase(this string input) =>
      string.IsNullOrEmpty(input) ? input :
      char.ToUpper(input[0]) + input[1..].ToLower();
}
