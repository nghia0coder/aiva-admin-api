namespace Aiva.Admin.Api.Core.ConversationAggregate;

public sealed class ChatRole : SmartEnum<ChatRole>
{
  public static readonly ChatRole System = new(nameof(System), 0);
  public static readonly ChatRole User = new(nameof(User), 1);
  public static readonly ChatRole Assistant = new(nameof(Assistant), 2);

  private ChatRole(string name, int value) : base(name, value) { }
}
