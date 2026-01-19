namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.ConversationAggregate;
using Core.UserAggregate;

public record GetConversationHistoryQuery(
    ConversationId ConversationId, 
    UserId UserId,
    PaginationParams? Pagination = null)
    : IQuery<Result<ConversationDetailDTO>>;

public record PaginationParams(
    Guid? BeforeMessageId = null,
    Guid? AfterMessageId = null,
    Guid? AroundMessageId = null,
    int Limit = 50)
{
    public LoadingMode GetLoadingMode()
    {
        if (AroundMessageId.HasValue) return LoadingMode.Around;
        if (BeforeMessageId.HasValue) return LoadingMode.Before;
        if (AfterMessageId.HasValue) return LoadingMode.After;
        return LoadingMode.Latest;
    }
}

public enum LoadingMode
{
    Latest,   // Load most recent messages (default, first load)
    Before,   // Load older messages (scroll up)
    After,    // Load newer messages (scroll down)
    Around    // Load context around specific message (search/deep link)
}
