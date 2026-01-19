namespace Aiva.Admin.Api.Infrastructure.Data.Queries;

using Core.ConversationAggregate;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Efficient query service for conversation messages with cursor-based pagination
/// Optimized for ChatGPT-like conversation loading patterns
/// </summary>
public class ConversationMessageQueryService(AppDbContext dbContext) : IConversationMessageQueryService
{
    public async Task<PaginatedMessages> GetLatestMessagesAsync(
        ConversationId conversationId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Get the most recent N messages, ordered by CreatedAt descending
        var messages = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit + 1) // Take one extra to check if there's more
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        if (hasMore)
        {
            messages = messages.Take(limit).ToList();
        }

        // Reverse to get chronological order
        messages.Reverse();

        return new PaginatedMessages(
            Messages: messages.AsReadOnly(),
            HasMore: hasMore,
            HasNewer: false);
    }

    public async Task<PaginatedMessages> GetMessagesBeforeAsync(
        ConversationId conversationId,
        Guid beforeMessageId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Get the anchor message to get its timestamp
        var anchorMessage = await dbContext.ChatMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == MessageId.From(beforeMessageId) && m.ConversationId == conversationId,
                cancellationToken);

        if (anchorMessage == null)
        {
            return new PaginatedMessages(
                Messages: Array.Empty<ChatMessage>(),
                HasMore: false,
                HasNewer: true);
        }

        // Get messages before the anchor timestamp
        // Use both timestamp and ID for stable sorting when timestamps are equal
        var messages = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && (m.CreatedAt < anchorMessage.CreatedAt
                         || (m.CreatedAt == anchorMessage.CreatedAt && m.Id.Value < beforeMessageId)))
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        if (hasMore)
        {
            messages = messages.Take(limit).ToList();
        }

        // Reverse to get chronological order
        messages.Reverse();

        return new PaginatedMessages(
            Messages: messages.AsReadOnly(),
            HasMore: hasMore,
            HasNewer: true);
    }

    public async Task<PaginatedMessages> GetMessagesAfterAsync(
        ConversationId conversationId,
        Guid afterMessageId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Get the anchor message
        var anchorMessage = await dbContext.ChatMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == MessageId.From(afterMessageId) && m.ConversationId == conversationId,
                cancellationToken);

        if (anchorMessage == null)
        {
            return new PaginatedMessages(
                Messages: Array.Empty<ChatMessage>(),
                HasMore: true,
                HasNewer: false);
        }

        // Get messages after the anchor timestamp
        var messages = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && (m.CreatedAt > anchorMessage.CreatedAt
                         || (m.CreatedAt == anchorMessage.CreatedAt && m.Id.Value > afterMessageId)))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(limit + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasNewer = messages.Count > limit;
        if (hasNewer)
        {
            messages = messages.Take(limit).ToList();
        }

        return new PaginatedMessages(
            Messages: messages.AsReadOnly(),
            HasMore: true,
            HasNewer: hasNewer);
    }

    public async Task<PaginatedMessages> GetMessagesAroundAsync(
        ConversationId conversationId,
        Guid aroundMessageId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Get the anchor message
        var anchorMessage = await dbContext.ChatMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == MessageId.From(aroundMessageId) && m.ConversationId == conversationId,
                cancellationToken);

        if (anchorMessage == null)
        {
            return new PaginatedMessages(
                Messages: Array.Empty<ChatMessage>(),
                HasMore: false,
                HasNewer: false);
        }

        var contextSize = limit / 2;

        // Get messages before anchor
        var messagesBefore = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && (m.CreatedAt < anchorMessage.CreatedAt
                         || (m.CreatedAt == anchorMessage.CreatedAt && m.Id.Value < aroundMessageId)))
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(contextSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        messagesBefore.Reverse();

        // Get anchor and messages after (including anchor)
        var messagesAfter = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && m.CreatedAt >= anchorMessage.CreatedAt)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(contextSize + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allMessages = messagesBefore.Concat(messagesAfter).ToList();

        var hasMore = messagesBefore.Count == contextSize;
        var hasNewer = messagesAfter.Count == contextSize + 1;

        return new PaginatedMessages(
            Messages: allMessages.AsReadOnly(),
            HasMore: hasMore,
            HasNewer: hasNewer);
    }

    public async Task<int> CountMessagesAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId, cancellationToken);
    }
}
