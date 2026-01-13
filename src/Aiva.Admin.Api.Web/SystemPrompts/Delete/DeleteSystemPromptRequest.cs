namespace Aiva.Admin.Api.Web.SystemPrompts.Delete;

public sealed class DeleteSystemPromptRequest
{
  public const string Route = "/SystemPrompts/{Id:int}";

  public int Id { get; set; }
}

