using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations;

public record ConversationDTO(
    ConversationId Id,
    string Title,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    int MessageCount);

public record ChatMessageDTO(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    MessageMetadataDTO? Metadata = null,
    string ResponseType = "text",
    TableDataDTO? StructuredData = null);

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

// Structured response DTOs for product tables
public record TableDataDTO(
    TableMetadataDTO Metadata,
    IReadOnlyList<TableColumnDTO> Columns,
    IReadOnlyList<TableRowDTO> Rows,
    IReadOnlyList<ActionMetadataDTO> GlobalActions);

public record TableMetadataDTO(
    string Title,
    string? Description,
    int TotalCount,
    int DisplayedCount);

public record TableColumnDTO(
    string Key,
    string Label,
    string Type,
    bool Sortable = true,
    bool Filterable = false);

public record TableRowDTO(
    string Id,
    IReadOnlyDictionary<string, object?> Cells,
    IReadOnlyList<ActionMetadataDTO> Actions);

public record ActionMetadataDTO(
    string Type,
    string Label,
    string? Icon,
    string Endpoint,
    string Method,
    IReadOnlyDictionary<string, object> Params,
    bool IsDisabled = false,
    string? DisabledReason = null);
