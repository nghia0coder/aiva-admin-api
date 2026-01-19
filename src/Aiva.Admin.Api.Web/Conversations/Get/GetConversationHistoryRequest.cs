namespace Aiva.Admin.Api.Web.Conversations.Get;

public class GetConversationHistoryRequest
{
    public const string Route = "/conversations/{id}";

    public Guid Id { get; set; }
    
    // Cursor-based pagination parameters
    public Guid? BeforeMessageId { get; set; }  // Load messages before this message ID (scroll up)
    public Guid? AfterMessageId { get; set; }   // Load messages after this message ID (scroll down)
    public Guid? AroundMessageId { get; set; }  // Load messages around this message ID (deep linking)
    
    // Page size with sensible default and maximum
    public int? Limit { get; set; } = 50;
    
    // Loading mode can be inferred from which cursor is set
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
