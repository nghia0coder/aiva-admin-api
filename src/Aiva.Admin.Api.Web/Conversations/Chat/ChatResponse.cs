namespace Aiva.Admin.Api.Web.Conversations.Chat;

public record ChatResponse(
    Guid MessageId,
    string Role,
    string Content,
    DateTime CreatedAt);
