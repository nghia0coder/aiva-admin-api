namespace Aiva.Admin.Api.Core.UserAggregate;

public sealed class UserRole : SmartEnum<UserRole>
{
  public static readonly UserRole Admin = new(nameof(Admin), 1);
  public static readonly UserRole Customer = new(nameof(Customer), 2);

  private UserRole(string name, int value) : base(name, value) { }
}
