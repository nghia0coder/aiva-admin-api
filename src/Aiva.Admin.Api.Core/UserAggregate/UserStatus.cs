namespace Aiva.Admin.Api.Core.UserAggregate;

public sealed class UserStatus : SmartEnum<UserStatus>
{
  public static readonly UserStatus Active = new(nameof(Active), 1);
  public static readonly UserStatus Inactive = new(nameof(Inactive), 2);
  public static readonly UserStatus Suspended = new(nameof(Suspended), 3);

  private UserStatus(string name, int value) : base(name, value) { }
}
