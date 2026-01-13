namespace Aiva.Admin.Api.Web.SystemPrompts.Get;

public sealed class GetSystemPromptRequest
{
  public const string Route = "/SystemPrompts/{Id:int}";

  public int Id { get; set; }
}

