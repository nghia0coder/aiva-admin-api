// Core/Interfaces/ICurrentUserService.cs
namespace Aiva.Admin.Api.Core.Interfaces;

public interface ICurrentUserService
{
  string? AzureAdObjectId { get; }
  string? Email { get; }
  string? DisplayName { get; }
  bool IsAuthenticated { get; }
}
