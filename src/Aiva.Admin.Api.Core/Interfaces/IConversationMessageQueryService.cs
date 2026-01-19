namespace Aiva.Admin.Api.Core.Interfaces;

using ConversationAggregate;

/// <summary>
/// Service for querying conversation messages with cursor-based pagination
/// </summary>
public interface IConversationMessageQueryService
{
    /// <summary>
    /// Get the most recent N messages from a conversation
    /// </summary>
    Task<PaginatedMessages> GetLatestMessagesAsync(
        ConversationId conversationId, 
        int limit, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages before a specific message (for scrolling up/loading older messages)
    /// </summary>
    Task<PaginatedMessages> GetMessagesBeforeAsync(
        ConversationId conversationId, 
        Guid beforeMessageId, 
        int limit, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages after a specific message (for scrolling down/loading newer messages)
    /// </summary>
    Task<PaginatedMessages> GetMessagesAfterAsync(
        ConversationId conversationId, 
        Guid afterMessageId, 
        int limit, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages around a specific message (for deep linking/search results)
    /// </summary>
    Task<PaginatedMessages> GetMessagesAroundAsync(
        ConversationId conversationId, 
        Guid aroundMessageId, 
        int limit, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count of messages in a conversation
    /// </summary>
    Task<int> CountMessagesAsync(
        ConversationId conversationId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a paginated message query
/// </summary>
public record PaginatedMessages(
    IReadOnlyList<ChatMessage> Messages,
    bool HasMore,
    bool HasNewer);
