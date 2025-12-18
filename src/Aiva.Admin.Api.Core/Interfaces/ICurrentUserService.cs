namespace Aiva.Admin.Api.Core.Interfaces;

using UserAggregate;

public interface ICurrentUserService
{
  UserId? UserId { get; }
  string? AzureAdObjectId { get; }
  string? Email { get; }
  string? DisplayName { get; }
  bool IsAuthenticated { get; }
}
