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
    ConversationMetadataDTO? Metadata = null);

public record ConversationMetadataDTO(
    int TotalMessages,
    int TotalTokens,
    DateTime LastActiveAt,
    string Status = "active",
    bool IsArchived = false);
