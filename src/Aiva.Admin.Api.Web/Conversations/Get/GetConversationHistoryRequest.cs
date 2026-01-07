namespace Aiva.Admin.Api.Web.Conversations.Get;

public class GetConversationHistoryRequest
{
    public const string Route = "/conversations/{id}";

    public Guid Id { get; set; }
}
