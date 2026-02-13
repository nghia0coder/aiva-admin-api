using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.UserAggregate;

public class User : AuditableEntity<User, UserId>, IAggregateRoot
{
  /// <summary>
  /// Azure AD Object ID (oid claim) - primary link to external identity
  /// </summary>
  public AzureAdObjectId AzureAdObjectId { get; private set; }

  /// <summary>
  /// User's email from Azure AD
  /// </summary>
  public string Email { get; private set; } = string.Empty;

  /// <summary>
  /// Display name from Azure AD
  /// </summary>
  public string DisplayName { get; private set; } = string.Empty;

  /// <summary>
  /// First name (givenName claim)
  /// </summary>
  public string? FirstName { get; private set; }

  /// <summary>
  /// Last name (surname claim)
  /// </summary>
  public string? LastName { get; private set; }

  /// <summary>
  /// User status in your system
  /// </summary>
  public UserStatus Status { get; private set; } = UserStatus.Active;

  /// <summary>
  /// User role in the system (Admin or Customer)
  /// </summary>
  public UserRole Role { get; private set; } = UserRole.Customer;

  /// <summary>
  /// Last successful login timestamp
  /// </summary>
  public DateTime? LastLoginAt { get; private set; }

  private User() { } // EF Core

  private User(AzureAdObjectId azureAdObjectId, string email, string displayName)
  {
    AzureAdObjectId = azureAdObjectId;
    Email = Guard.Against.NullOrWhiteSpace(email);
    DisplayName = Guard.Against.NullOrWhiteSpace(displayName);
  }

  public static User Create(
      AzureAdObjectId azureAdObjectId,
      string email,
      string displayName,
      string? firstName = null,
      string? lastName = null,
      UserRole? role = null)
  {
    var user = new User(azureAdObjectId, email, displayName)
    {
      FirstName = firstName,
      LastName = lastName,
      Role = role ?? UserRole.Customer
    };
    return user;
  }

  public User UpdateFromAzureAd(string email, string displayName, string? firstName, string? lastName)
  {
    Email = email;
    DisplayName = displayName;
    FirstName = firstName;
    LastName = lastName;
    return this;
  }

  public User RecordLogin()
  {
    LastLoginAt = DateTime.UtcNow;
    return this;
  }

  public User Deactivate()
  {
    Status = UserStatus.Inactive;
    return this;
  }

  public User ChangeRole(UserRole newRole)
  {
    Role = newRole;
    return this;
  }
}
