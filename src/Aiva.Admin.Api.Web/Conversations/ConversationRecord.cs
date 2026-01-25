using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.Web.Conversations;

public record ConversationRecord(
    ConversationId Id,
    string Title,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    int MessageCount);
