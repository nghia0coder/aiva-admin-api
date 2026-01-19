namespace Aiva.Admin.Api.UseCases.Conversations;

public record ConversationDTO(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    int MessageCount);

public record ChatMessageDTO(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    MessageMetadataDTO? Metadata = null);

public record MessageMetadataDTO(
    int TokenCount,
    TimeSpan? ResponseTime = null,
    string? Model = null,
    bool IsEdited = false,
    DateTime? EditedAt = null,
    string Status = "completed");

public record ConversationDetailDTO(
    Guid Id,
    string Title,
    string? SystemPrompt,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageDTO> Messages,
    ConversationMetadataDTO? Metadata = null,
    PaginationInfoDTO? Pagination = null);

public record ConversationMetadataDTO(
    int TotalMessages,
    int TotalTokens,
    DateTime LastActiveAt,
    string Status = "active",
    bool IsArchived = false);

public record PaginationInfoDTO(
    bool HasMore,              // More messages exist before oldest returned
    bool HasNewer,             // More messages exist after newest returned
    Guid? OldestMessageId,     // Cursor to fetch previous page
    Guid? NewestMessageId,     // Cursor to fetch next page
    DateTime? OldestTimestamp, // Alternative timestamp-based cursor
    DateTime? NewestTimestamp, // Alternative timestamp-based cursor
    int TotalMessages,         // Total messages in conversation (for UI)
    int ReturnedCount);        // Messages in this response
