namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamShoppingChatCommand(
    Guid ConversationId,
    string UserName,
    string Message,
    string? AdditionalUserData = null,
    IReadOnlyList<ChatImageUpload>? Images = null) : IRequest<Result<StreamShoppingChatResponse>>;

/// <summary>
/// Represents an image uploaded in chat
/// </summary>
public record ChatImageUpload(
    string FileName,
    string ContentType,
    System.IO.Stream ImageStream,
    long FileSizeBytes);
