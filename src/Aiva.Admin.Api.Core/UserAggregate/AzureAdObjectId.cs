using Vogen;

namespace Aiva.Admin.Api.Core.UserAggregate;

/// <summary>
/// Azure AD Object ID (oid claim) - the unique identifier for user in Azure AD
/// </summary>
[ValueObject<string>]
public readonly partial struct AzureAdObjectId
{
  private static Validation Validate(string value) =>
      string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out _)
          ? Validation.Invalid("Azure AD Object ID must be a valid GUID.")
          : Validation.Ok;
}
