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
    DateTime CreatedAt);

public record ConversationDetailDTO(
    Guid Id,
    string Title,
    string? SystemPrompt,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageDTO> Messages);
