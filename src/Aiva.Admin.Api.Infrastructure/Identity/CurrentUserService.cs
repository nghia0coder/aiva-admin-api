using System.Security.Claims;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Aiva.Admin.Api.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

  public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

  // Azure AD Object ID (oid claim)
  public string? AzureAdObjectId => User?.FindFirstValue("oid")
      ?? User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

  public string? Email => User?.FindFirstValue(ClaimTypes.Email)
      ?? User?.FindFirstValue("preferred_username")
      ?? User?.FindFirstValue("email");

  public string? DisplayName => User?.FindFirstValue("name")
      ?? User?.FindFirstValue(ClaimTypes.Name);
}
